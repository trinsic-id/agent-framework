using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Base agent implementation
    /// </summary>
    public abstract class AgentBase
    {
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>
        /// The service provider.
        /// </value>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentBase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        protected AgentBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the message handlers.
        /// </summary>
        /// <value>
        /// The handlers.
        /// </value>
        public abstract IEnumerable<IMessageHandler> Handlers { get; }

        /// <summary>
        /// Processes the asynchronous.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Expected inner message to be of type 'ForwardMessage'</exception>
        /// <exception cref="AgentFrameworkException">Couldn't locate a message handler for type {messageType}</exception>
        public async Task ProcessAsync(byte[] body, Wallet wallet, Pool pool = null)
        {
            var messageService = ServiceProvider.GetService<IMessageService>();

            var agentContext = new AgentContext {Wallet = wallet, Pool = pool};

            var outerMessageContext = await messageService.RecieveAsync(agentContext, body);

            ForwardMessage forwardMessage;
            try
            {
                forwardMessage = outerMessageContext.GetMessage<ForwardMessage>();
            }
            catch (Exception)
            {
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Expected outer message of type forward message");
            }

            var innerMessageContents = Convert.FromBase64String(forwardMessage.Message);
            var innerMessageContext = await messageService.RecieveAsync(agentContext, innerMessageContents);
            
            var handler = Handlers.FirstOrDefault(x =>
                x.SupportedMessageTypes.Any(y => y.Equals(innerMessageContext.MessageType, StringComparison.OrdinalIgnoreCase)));
            if (handler != null)
            {
                await handler.ProcessAsync(innerMessageContext);
            }
            else
            {
                throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                    $"Couldn't locate a message handler for type {innerMessageContext.MessageType}");
            }
        }
    }
}