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
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
using Indy = Hyperledger.Indy.PaymentsApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Handlers.Agents;
using System.IO;

namespace AgentFramework.Core.Tests
{
    public class PaymentTests : IAsyncLifetime
    {
        private WalletConfiguration _walletConfig = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        private WalletCredentials Credentials = new WalletCredentials { Key = "test" };
        private IAgentContext _context;
        private IHost _host;

        public async Task DisposeAsync()
        {
            await _host.StopAsync();

            await _context.Wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(_walletConfig.ToJson(), Credentials.ToJson());
        }

        public async Task InitializeAsync()
        {
            //_context = await AgentUtils.Create(_walletConfig.ToJson(), Credentials.ToJson(), true);

            _host = new HostBuilder()
                .ConfigureServices(services =>
                    services.AddAgentFramework(builder =>
                        builder
                            .AddBasicAgent(config =>
                            {
                                config.EndpointUri = new Uri("http://test");
                                config.SupportPayments = true;
                                config.WalletConfiguration = _walletConfig;
                                config.WalletCredentials = Credentials;
                                config.GenesisFilename = Path.GetFullPath("pool_genesis.txn");
                                config.PoolName = "TestPool";
                            })
                            .AddSovrinToken()))
                .Build();

            await _host.StartAsync();

            _context = await _host.Services.GetService<IAgentProvider>().GetContextAsync();
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
            try
            {
                await Pool.SetProtocolVersionAsync(2);

                var trustee1 = await Did.CreateAndStoreMyDidAsync(_context.Wallet,
                    new { seed = "000000000000000000000000Trustee1" }.ToJson());

                async Task<CreateAndStoreMyDidResult> PromoteTrustee(string seed)
                {
                    var trustee = await Did.CreateAndStoreMyDidAsync(_context.Wallet, new { seed = seed }.ToJson());
                    var nymRequest =
                        await Ledger.BuildNymRequestAsync(trustee1.Did, trustee.Did, trustee.VerKey, null, "TRUSTEE");
                    var nymResponse = await Ledger.SignAndSubmitRequestAsync(await _context.Pool, _context.Wallet,
                        trustee1.Did, nymRequest);
                    return trustee;
                }

                var paymentService = _host.Services.GetService<IPaymentService>();
                var address = await paymentService.CreatePaymentAddressAsync(_context);

                var trustee2 = await PromoteTrustee("000000000000000000000000Trustee2");
                var trustee3 = await PromoteTrustee("000000000000000000000000Trustee3");

                var amount = (ulong)new Random().Next(100, int.MaxValue);
                var request = await Indy.Payments.BuildMintRequestAsync(_context.Wallet, trustee1.Did,
                    new[] { new { recipient = address.Address, amount = amount } }.ToJson(), null);

                var singedRequest1 = await Ledger.MultiSignRequestAsync(_context.Wallet, trustee1.Did, request.Result);
                var singedRequest2 = await Ledger.MultiSignRequestAsync(_context.Wallet, trustee2.Did, singedRequest1);
                var singedRequest3 = await Ledger.MultiSignRequestAsync(_context.Wallet, trustee3.Did, singedRequest2);

                await Ledger.SubmitRequestAsync(await _context.Pool, singedRequest3);

                var totalAmount = await paymentService.GetBalanceAsync(_context, address);

                var address2 = await paymentService.CreatePaymentAddressAsync(_context, new PaymentAddressConfiguration
                {
                    AccountId = "000000000000000000000000Account1"
                });

                var paymentRecord = new PaymentRecord
                {
                    Address = address2.Address,
                    Amount = (ulong)new Random().Next(100)
                };
                await paymentService.MakePaymentAsync(_context, paymentRecord, address);

                Assert.Equal(totalAmount.Value, amount);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
