using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Helpers;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Runtime;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class WalletTests
    {
        [Fact]
        public async Task ProvisionNewWallet()
        {
            var walletService = new DefaultWalletService();
            var provisioningService = new DefaultProvisioningService(
                new DefaultWalletRecordService(new DateTimeHelper()), walletService);

            var config = new WalletConfiguration {Id = Guid.NewGuid().ToString()};
            var creds = new WalletCredentials {Key = "1"};

            await provisioningService.ProvisionAgentAsync(new ProvisioningConfiguration
            {
                WalletConfiguration = config,
                WalletCredentials = creds,
                EndpointUri = new Uri("http://mock")
            });

            var wallet = await walletService.GetWalletAsync(config, creds);
            Assert.NotNull(wallet);

            var provisioning = await provisioningService.GetProvisioningAsync(wallet);

            Assert.NotNull(provisioning);
            Assert.NotNull(provisioning.Endpoint);
            Assert.NotNull(provisioning.Endpoint.Did);
            Assert.NotNull(provisioning.Endpoint.Verkey);
        }
    }
}
