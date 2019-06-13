using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Handlers.Internal
{
    public class PaymentsAgentMiddleware : IAgentMiddleware
    {
        public PaymentsAgentMiddleware()
        {
        }

        public Task ProcessMessageAsync(IAgentContext agentConext, MessageContext messageContext)
        {
            throw new NotImplementedException();
        }
    }
}
