using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Messages;

namespace AgentFramework.Payments.Middleware
{
    public class PaymentsAgentMiddleware : IAgentMiddleware
    {
        public Task ProcessMessageAsync(IAgentContext agentConext, MessageContext messageContext)
        {
            throw new NotImplementedException();
        }
    }
}
