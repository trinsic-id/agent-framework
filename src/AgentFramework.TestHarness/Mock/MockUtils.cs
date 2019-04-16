using System;
using System.Net.Http;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.TestHarness.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.TestHarness.Mock
{
    public class MockUtils
    {
        public static async Task<MockAgent> CreateAsync(string agentName, WalletConfiguration configuration, WalletCredentials credentials, MockAgentHttpHandler handler, string issuerSeed = null)
        {
            var services = new ServiceCollection();

            services.AddAgentFramework();
            services.AddLogging();
            services.AddSingleton<MockAgentMessageProcessor>();
            services.AddSingleton<HttpMessageHandler>(handler);
            services.AddSingleton(p => new HttpClient(p.GetRequiredService<HttpMessageHandler>()));

            return await CreateAsync(agentName, configuration, credentials, services, issuerSeed);
        }

        public static async Task<MockAgent> CreateAsync(string agentName, WalletConfiguration configuration, WalletCredentials credentials, ServiceCollection services, string issuerSeed = null)
        {
            var provider = services.BuildServiceProvider();

            await provider.GetService<IProvisioningService>()
                .ProvisionAgentAsync(new ProvisioningConfiguration { WalletConfiguration = configuration, WalletCredentials = credentials, EndpointUri = new Uri($"http://{agentName}"), IssuerSeed = issuerSeed, CreateIssuer = issuerSeed != null });

            return new MockAgent(agentName, provider)
            {
                Context = new AgentContext { Wallet = await provider.GetService<IWalletService>().GetWalletAsync(configuration, credentials), Pool = new PoolAwaitable(PoolUtils.GetPoolAsync) },
                ServiceProvider = provider
            };
        }

        public static async Task Dispose(MockAgent agent)
        {
            agent.Context.Wallet.Dispose();
            await agent.Context.Wallet.CloseAsync();
        }
    }
}
