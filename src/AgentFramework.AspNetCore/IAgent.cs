using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;

namespace AgentFramework.AspNetCore.Middleware
{
    public interface IAgent
    {
        Task<MessageResponse> ProcessAsync(IAgentContext context, MessageContext messageContext);

        IList<IMessageHandler> Handlers { get; }
    }
}