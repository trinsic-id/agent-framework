using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// A convenience base class for implementing strong type handlers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="AgentFramework.Core.Handlers.IMessageHandler" />
    public abstract class MessageHandlerBase<T> : IMessageHandler
        where T : IAgentMessage, new()
    {
        private readonly string _supportedMessageType;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerBase{T}"/> class.
        /// </summary>
        protected MessageHandlerBase()
        {
            _supportedMessageType = new T().Type;
        }

        /// <inheritdoc />
        public IEnumerable<string> SupportedMessageTypes => new[] { _supportedMessageType };

        /// <summary>
        /// Processes the incoming <see cref="IAgentMessage"/>
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="agentContext">The message agentContext.</param>
        /// <returns></returns>
        protected abstract Task ProcessAsync(T message, IAgentContext agentContext);

        /// <inheritdoc />
        public Task ProcessAsync(IAgentContext agentContext, MessagePayload messagePayload) =>
            ProcessAsync(messagePayload.GetMessage<T>(), agentContext);
    }
}
