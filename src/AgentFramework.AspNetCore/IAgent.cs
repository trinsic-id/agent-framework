using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;

namespace AgentFramework.AspNetCore.Middleware
{
    /// <summary>
    /// Agent.
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Processes the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="context">Context.</param>
        /// <param name="messageContext">Message context.</param>
        Task<MessageResponse> ProcessAsync(IAgentContext context, MessageContext messageContext);

        /// <summary>
        /// Gets the handlers.
        /// </summary>
        /// <value>The handlers.</value>
        IList<IMessageHandler> Handlers { get; }
    }
}