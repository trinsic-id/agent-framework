using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Common;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Handlers.Internal
{
    public class DefaultBasicMessageHandler : MessageHandlerBase<BasicMessage>
    {
        private readonly IWalletRecordService _recordService;

        public DefaultBasicMessageHandler(IWalletRecordService recordService)
        {
            _recordService = recordService;
        }

        protected override async Task<AgentMessage> ProcessAsync(BasicMessage message, IAgentContext agentContext, MessageContext messageContext)
        {
            var record = new BasicMessageRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = messageContext.Connection.Id,
                Text = message.Content,
                SentTime = DateTime.TryParse(message.SentTime, out var dateTime) ? dateTime : DateTime.UtcNow,
                Direction = MessageDirection.Incoming
            };
            await _recordService.AddAsync(agentContext.Wallet, record);
            messageContext.ContextRecord = record;

            return null;
        }
    }
}
