using AgentFramework.Core.Contracts;
using System.Threading.Tasks;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class OutgoingMessageHandler : MessageHandlerBase<OutgoingMessage>
    {
        protected override Task ProcessAsync(OutgoingMessage message, IAgentContext agentContext)
        {
            // check outgoing endpoints in provisioning record

            if (agentContext is AgentContext context) 
                context.AddNext(new MessagePayload(new HttpOutgoingMessage { Message = message.Message }));

            return Task.CompletedTask;
        }
    }
}