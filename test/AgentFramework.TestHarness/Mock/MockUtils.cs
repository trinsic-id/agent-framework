using System;
using System.Net.Http;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models.Wallets;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.TestHarness.Mock
{
    public class MockUtils
    {
        public static async Task<MockAgent> CreateAsync(WalletConfiguration configuration, WalletCredentials credentials)
        {
            IAgentContext context = null;
            MockAgentMessageProcessor agentMessageProcessor = null;

            var handler = new MockAgentHttpHandler(data => agentMessageProcessor.HandleAsync(data, context));

            var container = new ServiceCollection();
            container.AddAgentFramework();
            container.AddLogging();
            container.AddSingleton<MockAgentMessageProcessor>();
            container.AddSingleton<HttpMessageHandler>(handler);
            container.AddSingleton(p => new HttpClient(p.GetRequiredService<HttpMessageHandler>()));
            var provider = container.BuildServiceProvider();

            await provider.GetService<IProvisioningService>()
                          .ProvisionAgentAsync(new ProvisioningConfiguration { WalletConfiguration = configuration, WalletCredentials = credentials, EndpointUri = new Uri("http://mock") });

            agentMessageProcessor = provider.GetService<MockAgentMessageProcessor>();

            context = new AgentContext { Wallet = await provider.GetService<IWalletService>().GetWalletAsync(configuration, credentials) };

            return new MockAgent
            {
                Context = context,
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
