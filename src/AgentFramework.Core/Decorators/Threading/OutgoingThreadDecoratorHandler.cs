using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;

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
        /// <param name="message">Outgoing message.</param>
        /// <param name="agentContext"></param>
        /// <returns></returns>
        public Task<OutgoingMessage> ProcessAsync(OutgoingMessage message, IAgentContext agentContext)
        {
            //TODO I probably want to be able to set this type of thing from the particular outgoing message, ie only add a certain decorator under a certain condition?

            if (message.OutboundMessage == null)
                throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                    "Cannot apply decorator when outbound message is null");

            if (message.InboundMessage == null)
                return Task.FromResult(message);

            ThreadDecorator previousMessageThreadContext = null;
            string previousMessageId = null;

            try
            {
                previousMessageThreadContext = message.InboundMessage.GetDecorator<ThreadDecorator>(DecoratorIdentifier);
                previousMessageId = message.InboundMessage.GetMessageId();
            }
            catch (AgentFrameworkException) { }

            ThreadDecorator currentThreadContext = null;

            if (previousMessageThreadContext != null)
            {
                currentThreadContext = new ThreadDecorator
                {
                    ParentThreadId = previousMessageThreadContext.ParentThreadId,
                    ThreadId = previousMessageThreadContext.ThreadId
                };
            }
            else if (!string.IsNullOrEmpty(previousMessageId))
            {
                currentThreadContext = new ThreadDecorator
                {
                    ThreadId = previousMessageId
                };
            }

            if (currentThreadContext != null)
                message.OutboundMessage.AddDecorator(currentThreadContext, DecoratorIdentifier);

            return Task.FromResult(message);
        }
    }
}
