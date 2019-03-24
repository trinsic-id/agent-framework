using System;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.TestHarness;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.Core.Tests.Integration
{
    public class CredentialTests : IAsyncLifetime
    {
        WalletConfiguration config1 = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        WalletConfiguration config2 = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        WalletCredentials cred = new WalletCredentials { Key = "2" };

        private MockAgent _issuerAgent;
        private MockAgent _holderAgent;
        private readonly MockAgentRouter _router = new MockAgentRouter();

        public async Task InitializeAsync()
        {
            _issuerAgent = await MockUtils.CreateAsync("issuer", config1, cred, new MockAgentHttpHandler((name, data) => _router.RouteMessage(name, data)), "000000000000000000000000Steward1");
            _router.RegisterAgent(_issuerAgent);
            _holderAgent = await MockUtils.CreateAsync("holder", config2, cred, new MockAgentHttpHandler((name, data) => _router.RouteMessage(name, data)));
            _router.RegisterAgent(_holderAgent);
        }

        [Fact]
        public async Task CanIssueCredential()
        {
            (var issuerConnection, var holderConnection)  = await AgentScenarios.EstablishConnectionAsync(_issuerAgent, _holderAgent);
            await AgentScenarios.IssueCredential(_issuerAgent, _holderAgent, issuerConnection, holderConnection);
        }

        public async Task DisposeAsync()
        {
            await _issuerAgent.Dispose();
            await _holderAgent.Dispose();
        }
    }
}
