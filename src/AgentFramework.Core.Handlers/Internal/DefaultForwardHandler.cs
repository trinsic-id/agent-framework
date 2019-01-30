using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages.Routing;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class DefaultForwardHandler : MessageHandlerBase<ForwardMessage>
    {
        private readonly IConnectionService _connectionService;

        public DefaultForwardHandler(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        protected override async Task ProcessAsync(ForwardMessage message, IAgentContext agentContext)
        {
            var connectionRecord = await _connectionService.ResolveByMyKeyAsync(agentContext.Wallet, message.To);
            agentContext.Connection = connectionRecord;

            if (agentContext is AgentContext context) 
                context.AddNext(new MessagePayload(message.Message, true));
        }
    }
}
