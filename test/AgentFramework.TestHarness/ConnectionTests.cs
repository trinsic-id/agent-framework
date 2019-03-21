using System;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.TestHarness
{
    public class ConnectionTests : IAsyncLifetime
    {
        WalletConfiguration config1 = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        WalletConfiguration config2 = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        WalletCredentials cred = new WalletCredentials { Key = "2" };

        private MockAgent _agent1;
        private MockAgent _agent2;
        
        public async Task InitializeAsync()
        {
            _agent1 = await MockUtils.CreateAsync(config1, cred);
            _agent2 = await MockUtils.CreateAsync(config2, cred);
        }

        [Fact]
        public async Task ConnectUsingHttp()
        {
            await Scenarios.EstablishConnectionAsync(_agent1, _agent2);
        }

        public async Task DisposeAsync()
        {
            await _agent1.Dispose();
            await _agent2.Dispose();
        }
    }
}