using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models;

namespace AgentFramework.Core.Tests
{
    public class MockMessageHandler : IMessageHandler
    {
        public IEnumerable<string> SupportedMessageTypes { get; }

        public Task ProcessAsync(MessagePayload messagePayload, IAgentContext agentContext)
        {
            throw new NotImplementedException();
        }
    }
}
