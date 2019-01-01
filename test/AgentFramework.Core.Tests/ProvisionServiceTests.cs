using System;
using System.Threading.Tasks;
using AgentFramework.Core.Helpers;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Runtime;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class ProvisionServiceTests
    {
        [Fact]
        public async Task CanProvisionAgent()
        {
            var walletService = new DefaultWalletService();
            var provisioningService = new DefaultProvisioningService(
                new DefaultWalletRecordService(new DateTimeHelper()), walletService);

            var config = new WalletConfiguration {Id = Guid.NewGuid().ToString()};
            var creds = new WalletCredentials {Key = "1"};

            await provisioningService.ProvisionAgentAsync(new ProvisioningConfiguration(
                config,
                creds,
                new AgentService
                {
                    Id = "test",
                    ServiceEndpoint = "https://mock-endpoint.com"
                }));

            var wallet = await walletService.GetWalletAsync(config, creds);
            Assert.NotNull(wallet);

            var provisioning = await provisioningService.GetProvisioningAsync(wallet);

            Assert.NotNull(provisioning);
            Assert.NotNull(provisioning.Services);

            Assert.IsType<AgentService>(provisioning.Services[0]);

            var service = provisioning.Services[0] as AgentService;

            Assert.NotNull(service?.ServiceEndpoint);
        }

        [Fact]
        public void ProvisionConfigThrowsArgumentNullExceptions()
        {
            var config = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
            var creds = new WalletCredentials { Key = "1" };

            Assert.Throws<ArgumentNullException>(() => new ProvisioningConfiguration(null, creds, new AgencyService()));
            Assert.Throws<ArgumentNullException>(() => new ProvisioningConfiguration(config, null, new AgencyService()));
            Assert.Throws<ArgumentNullException>(() => new ProvisioningConfiguration(config, creds, new IDidService[] {}));
        }

        [Fact]
        public async Task ProvisionAgentThrowsArgumentNullException()
        {
            var walletService = new DefaultWalletService();
            var provisioningService = new DefaultProvisioningService(
                new DefaultWalletRecordService(new DateTimeHelper()), walletService);
            
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await provisioningService.ProvisionAgentAsync(null));
        }
    }
}
