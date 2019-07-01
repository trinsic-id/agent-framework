using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Handlers.Agents
{
    public interface IAgentMiddleware
    {
        Task OnMessageAsync(IAgentContext agentConext, MessageContext messageContext);
    }
}
