using System;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.TestHarness;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.Core.Tests.Protocols
{
    public class AgentConnectionTests : IAsyncLifetime
    {
        WalletConfiguration config1 = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        WalletConfiguration config2 = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        WalletCredentials cred = new WalletCredentials { Key = "2" };

        private MockAgent _agent1;
        private MockAgent _agent2;
        private readonly MockAgentRouter _router = new MockAgentRouter();

        public async Task InitializeAsync()
        {
            _agent1 = await MockUtils.CreateAsync("agent1", config1, cred, new MockAgentHttpHandler((name, data) => _router.RouteMessage(name, data)));
            _router.RegisterAgent(_agent1);
            _agent2 = await MockUtils.CreateAsync("agent2", config2, cred, new MockAgentHttpHandler((name, data) => _router.RouteMessage(name, data)));
            _router.RegisterAgent(_agent2);
        }

        [Fact]
        public async Task ConnectUsingHttp()
        {
            await AgentScenarios.EstablishConnectionAsync(_agent1, _agent2);
        }

        public async Task DisposeAsync()
        {
            await _agent1.Dispose();
            await _agent2.Dispose();
        }
    }
}
