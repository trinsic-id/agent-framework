using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.TestHarness.Mock
{
    public class MockAgent : AgentMessageProcessorBase
    {
        public MockAgent(string name, IServiceProvider provider) : base(provider)
        {
            Name = name;
        }

        public string Name { get; }

        public IAgentContext Context { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public T GetService<T>() => ServiceProvider.GetRequiredService<T>();

        public Task<byte[]> HandleInboundAsync(byte[] data) => ProcessAsync(data, Context);

        public async Task Dispose() => await Context.Wallet.CloseAsync();

        protected override void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddForwardHandler();
            AddCredentialHandler();
            AddProofHandler();
        }
    }
}
