using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models.Messaging;

namespace AgentFramework.AspNetCore.Tests
{
    public class MockMessageHandler : IMessageHandler
    {
        public IEnumerable<string> SupportedMessageTypes { get; }

        public Task ProcessAsync(MessageContext agentMessageContext)
        {
            throw new NotImplementedException();
        }
    }
}
