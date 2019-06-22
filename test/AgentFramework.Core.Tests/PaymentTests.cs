using System;
using System.Threading.Tasks;
using AgentFramework.AspNetCore;
using AgentFramework.Core.Handlers;
using AgentFramework.Payments.Abstractions;
using AgentFramework.Payments.SovrinToken;
using AgentFramework.TestHarness.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

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

            try
            {

            }
            catch (Exception e)
            {

            }
        }
    }
}
