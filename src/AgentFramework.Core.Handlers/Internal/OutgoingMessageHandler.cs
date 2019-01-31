using AgentFramework.Core.Contracts;
using System.Threading.Tasks;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class OutgoingMessageHandler : MessageHandlerBase<OutgoingMessage>
    {
        protected override async Task ProcessAsync(OutgoingMessage message, IAgentContext agentContext)
        {
            // check outgoing endpoints in connection record

            var inner = await CryptoUtils.PackAsync(
                agentContext.Wallet, agentContext.Connection.TheirVk, agentContext.Connection.MyVk, message.Message.GetUTF8Bytes());

            if (agentContext is AgentContext context) 
                context.AddNext(new MessagePayload(new HttpOutgoingMessage { Message = inner.GetUTF8String() }));
        }
    }
}