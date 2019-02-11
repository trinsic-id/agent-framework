using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Decorators
{
    /// <summary>
    /// Outgoing message decorator
    /// </summary>
    public interface IOutgoingMessageDecoratorHandler
    {
        /// <summary>
        /// The decorator identifier.
        /// </summary>
        string DecoratorIdentifier { get; }

        /// <summary>
        /// Processes the outgoing message.
        /// </summary>
        /// <param name="message">The outgoing message.</param>
        /// <param name="agentContext">The agent context.</param>
        /// <returns></returns>
        Task<OutgoingMessage> ProcessAsync(OutgoingMessage message, IAgentContext agentContext);
    }
}
