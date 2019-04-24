﻿using System.Collections.Generic;
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
        where T : AgentMessage, new()
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
        /// Processes the incoming <see cref="AgentMessage"/>
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="agentContext">The message agentContext.</param>
        /// <returns></returns>
        protected abstract Task<AgentMessage> ProcessAsync(T message, IAgentContext agentContext);

        /// <inheritdoc />
        public Task<AgentMessage> ProcessAsync(IAgentContext agentContext, IMessageContext messageContext) =>
            ProcessAsync(messageContext.GetMessage<T>(), agentContext);
    }
}
