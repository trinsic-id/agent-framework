using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Messaging;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Message handler interface
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        IEnumerable<string> SupportedMessageTypes { get; }

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentMessageContext">The agent message context.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        Task ProcessAsync(MessageContext agentMessageContext, ConnectionContext context);
    }
}