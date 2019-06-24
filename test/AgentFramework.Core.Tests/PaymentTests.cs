using System;
using System.Threading.Tasks;
using AgentFramework.AspNetCore;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Payments.SovrinToken;
using AgentFramework.TestHarness.Utils;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using AgentFramework.Core.Extensions;
using Indy = Hyperledger.Indy.PaymentsApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;

namespace AgentFramework.Core.Tests
{
    public class PaymentTests : IAsyncLifetime
    {
        private readonly string _walletConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private AgentContext _context;
        private IHost _host;

        public async Task DisposeAsync()
        {
            await _host.StopAsync();

            await _context.Wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(_walletConfig, Credentials);
        }

        public async Task InitializeAsync()
        {
            _context = await AgentUtils.Create(_walletConfig, Credentials, true);

            _host = new HostBuilder()
                .ConfigureServices(services =>
                    services.AddAgentFramework(builder =>
                        builder.AddSovrinToken()))
                .Build();

            await _host.StartAsync();
        }

        [Fact(DisplayName = "Create random payment address for Sovrin method")]
        public async Task CreateSovrinPaymentAddress()
        {
            var paymentService = _host.Services.GetService<IPaymentService>();
            var address = await paymentService.CreatePaymentAddressAsync(_context);

            Assert.NotNull(address);
            Assert.NotNull(address.Address);
        }

        [Fact(DisplayName = "Mint Sovrin tokens")]
        public async Task MintSovrinTokens()
        {
            await Pool.SetProtocolVersionAsync(2);

            var trustee1 = await Did.CreateAndStoreMyDidAsync(_context.Wallet, new { seed = "000000000000000000000000Trustee1" }.ToJson());

            async Task<CreateAndStoreMyDidResult> promoteTrustee(string seed)
            {
                var trustee = await Did.CreateAndStoreMyDidAsync(_context.Wallet, new { seed = seed }.ToJson());
                var nymRequest = await Ledger.BuildNymRequestAsync(trustee1.Did, trustee.Did, trustee.VerKey, null, "TRUSTEE");
                var nymResponse = await Ledger.SignAndSubmitRequestAsync(await _context.Pool, _context.Wallet, trustee1.Did, nymRequest);
                return trustee;
            }
            var paymentService = _host.Services.GetService<IPaymentService>();
            var address = await paymentService.CreatePaymentAddressAsync(_context);

            var trustee2 = await promoteTrustee("000000000000000000000000Trustee2");
            var trustee3 = await promoteTrustee("000000000000000000000000Trustee3");

            ulong amount = 1234560000;
            var request = await Indy.Payments.BuildMintRequestAsync(_context.Wallet, trustee1.Did, new[] { new { recipient = address.Address, amount = amount } }.ToJson(), null);

            var singedRequest1 = await Ledger.MultiSignRequestAsync(_context.Wallet, trustee1.Did, request.Result);
            var singedRequest2 = await Ledger.MultiSignRequestAsync(_context.Wallet, trustee2.Did, singedRequest1);
            var singedRequest3 = await Ledger.MultiSignRequestAsync(_context.Wallet, trustee3.Did, singedRequest2);

            await Ledger.SubmitRequestAsync(await _context.Pool, singedRequest3);

            var totalAmount = await paymentService.GetBalanceAsync(_context, address);

            Assert.Equal(totalAmount.Value, amount.ToString());
        }
    }
}
