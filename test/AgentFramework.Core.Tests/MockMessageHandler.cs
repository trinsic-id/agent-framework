using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Tests
{
    public class MockMessageHandler : IMessageHandler
    {
        public IEnumerable<string> SupportedMessageTypes { get; }
        public Task<AgentMessage> ProcessAsync(IAgentContext agentContext, IMessageContext messageContext)
        {
            throw new NotImplementedException();
        }
    }
}
