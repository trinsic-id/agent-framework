using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;

namespace AgentFramework.AspNetCore
{
    /// <summary>
    /// Default agent.
    /// </summary>
    public class DefaultAgent : AgentMessageProcessorBase, IAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.AspNetCore.DefaultAgent"/> class.
        /// </summary>
        /// <param name="provider">Provider.</param>
        public DefaultAgent(IServiceProvider provider) : base(provider)
        {
        }

        /// <summary>
        /// Gets the handlers.
        /// </summary>
        /// <value>The handlers.</value>
        public IList<IMessageHandler> Handlers => _handlers;

        /// <summary>
        /// Configures the handlers.
        /// </summary>
        protected override void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddForwardHandler();
        }

        async Task<MessageResponse> IAgent.ProcessAsync(IAgentContext context, MessageContext messageContext)
        {
            var response = new MessageResponse();
            response.Write(await ProcessAsync(context, messageContext));

            return response;
        }
    }
}
