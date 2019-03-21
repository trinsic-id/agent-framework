using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.TestHarness.Mock
{
    public class MockAgent
    {
        public IAgentContext Context { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public T GetService<T>() => ServiceProvider.GetRequiredService<T>();

        public async Task Dispose()
        {
            Context.Wallet.Dispose();
            await Context.Wallet.CloseAsync();
        }
    }
}
