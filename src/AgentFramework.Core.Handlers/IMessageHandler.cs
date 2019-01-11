using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// <param name="agentMessage">The agent message.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        Task ProcessAsync(string agentMessage, ConnectionContext context);
    }
}