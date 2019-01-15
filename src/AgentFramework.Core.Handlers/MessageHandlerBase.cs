using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Messaging;
using AgentFramework.Core.Models.Records;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// A base class for implementing single message type handlers.
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
        /// <param name="context">The message context.</param>
        /// <param name="connection">The associated connection.</param>
        /// <returns></returns>
        protected abstract Task ProcessAsync(T message, AgentContext context, ConnectionRecord connection);

        /// <inheritdoc />
        public Task ProcessAsync(MessageContext messageContext) =>
            ProcessAsync(messageContext.GetMessage<T>(), messageContext.AgentContext, messageContext.Connection);
    }
}
