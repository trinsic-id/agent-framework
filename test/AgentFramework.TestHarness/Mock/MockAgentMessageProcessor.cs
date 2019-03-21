using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;

namespace AgentFramework.TestHarness.Mock
{
    public class MockAgentMessageProcessor : AgentMessageProcessorBase
    {
        public MockAgentMessageProcessor(
            IServiceProvider provider) : base(provider)
        {
        }

        internal Task HandleAsync(byte[] data, IAgentContext context) => ProcessAsync(data, context);
    }
}
