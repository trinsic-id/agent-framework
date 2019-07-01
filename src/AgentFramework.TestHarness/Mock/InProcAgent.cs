using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AgentFramework.AspNetCore;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Payments.SovrinToken;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AgentFramework.TestHarness.Mock
{
    public class InProcAgent : AgentBase, IAsyncLifetime
    {
        /// <inheritdoc />
        public InProcAgent(IHost host) 
            : base(host.Services.GetService<IServiceProvider>())
        {
            Host = host;
        }

        public IHost Host { get; }
        public IAgentContext Context { get; private set; }

        internal Task<MessageResponse> HandleAsync(byte[] data) => 
            ProcessAsync(Context, new MessageContext(data, true));

        /// <inheritdoc />
        protected override void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddCredentialHandler();
            AddDiscoveryHandler();
            AddDiscoveryHandler();
            AddForwardHandler();
            AddProofHandler();
            AddEphemeralChallengeHandler();
        }

        #region Factory methods

        public static async Task<(InProcAgent agent1, InProcAgent agent2)> CreatePairedAsync()
        {
            var handler1 = new InProcMessageHandler();
            var handler2 = new InProcMessageHandler();

            var agent1 = Create(handler1);
            var agent2 = Create(handler2);

            handler1.TargetAgent = agent2;
            handler2.TargetAgent = agent1;

            await agent1.InitializeAsync();
            await agent2.InitializeAsync();

            return (agent1, agent2);
        }

        private static InProcAgent Create(HttpMessageHandler handler) =>
            new InProcAgent(new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options =>
                        options.SuppressStatusMessages = true);
                    services.AddAgentFramework(builder =>
                        builder.AddIssuerAgent(config =>
                            {
                                config.EndpointUri = new Uri("http://test");
                                config.WalletConfiguration = new WalletConfiguration {Id = Guid.NewGuid().ToString()};
                                config.WalletCredentials = new WalletCredentials {Key = "test"};
                                config.GenesisFilename = Path.GetFullPath("pool_genesis.txn");
                                config.PoolName = "TestPool";
                            })
                            .AddSovrinToken());
                    services.AddSingleton(handler);
                }).Build());

        #endregion

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            await Host.StartAsync();
            Context = await Host.Services.GetService<IAgentProvider>().GetContextAsync();
        }

        /// <inheritdoc />
        public Task DisposeAsync() => Host.StopAsync(TimeSpan.FromSeconds(10));
    }
}