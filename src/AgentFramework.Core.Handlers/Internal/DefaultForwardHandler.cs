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

        protected override async Task ProcessAsync(ForwardMessage message, AgentContext context)
        {
            var connectionRecord = await _connectionService.ResolveByMyKeyAsync(context.Wallet, message.To);
            context.Connection = connectionRecord;

            context.AddMessage(new MessagePayload(message.Message, true));
        }
    }
}
