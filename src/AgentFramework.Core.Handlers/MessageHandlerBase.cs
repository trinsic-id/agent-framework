using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// 
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
        public IEnumerable<string> SupportedMessageTypes => new[] {_supportedMessageType};

        /// <summary>
        /// Processes the incoming <see cref="IAgentMessage"/>
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The message context.</param>
        /// <returns></returns>
        protected abstract Task ProcessAsync(T message, ConnectionContext context);

        /// <inheritdoc />
        public Task ProcessAsync(string agentMessage, ConnectionContext context) =>
            ProcessAsync(JsonConvert.DeserializeObject<T>(agentMessage), context);
    }
}