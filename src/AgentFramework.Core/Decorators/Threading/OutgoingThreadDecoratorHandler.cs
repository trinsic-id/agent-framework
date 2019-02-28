using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Decorators.Threading
{
    /// <summary>
    /// Outgoing thread decorator handler
    /// </summary>
    public class OutgoingThreadDecoratorHandler : IOutgoingMessageDecoratorHandler
    {
        /// <inheritdoc />
        public string DecoratorIdentifier => "thread";

        /// <inheritdoc />
        /// <summary>
        /// Processes adding threading to an outgoing message.
        /// </summary>
        /// <param name="messageContext">Outgoing message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <returns></returns>
        public Task<OutgoingMessageContext> ProcessAsync(OutgoingMessageContext messageContext, Wallet wallet)
        {
            //TODO probably want to be able to set this type of thing from the particular outgoing message, ie only add a certain decorator under a certain condition?

            if (messageContext.OutboundMessage == null)
                throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                    "Cannot apply decorator when outbound message is null");

            if (messageContext.InboundMessage == null)
                return Task.FromResult(messageContext);

            ThreadDecorator previousMessageThreadContext = null;
            try
            {
                previousMessageThreadContext = messageContext.InboundMessage.GetDecorator<ThreadDecorator>(DecoratorIdentifier);
            }
            catch (AgentFrameworkException) { }

            ThreadDecorator currentThreadContext;
            if (previousMessageThreadContext != null)
            {
                currentThreadContext = new ThreadDecorator
                {
                    ParentThreadId = previousMessageThreadContext.ParentThreadId,
                    ThreadId = previousMessageThreadContext.ThreadId
                };
            }
            else
            {
                currentThreadContext = new ThreadDecorator
                {
                    ThreadId = messageContext.InboundMessage.Id
                };
            }

            messageContext.OutboundMessage.AddDecorator(currentThreadContext, DecoratorIdentifier);
            return Task.FromResult(messageContext);
        }
    }
}
