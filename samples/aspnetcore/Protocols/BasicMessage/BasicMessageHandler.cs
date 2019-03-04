using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;

namespace WebAgent.Protocols.BasicMessage
{
    public class BasicMessageHandler : MessageHandlerBase<BasicMessage>
    {
        private readonly IWalletRecordService _recordService;

        public BasicMessageHandler(IWalletRecordService recordService)
        {
            _recordService = recordService;
        }

        protected override async Task<AgentMessage> ProcessAsync(BasicMessage message, IAgentContext context)
        {
            Console.WriteLine($"Processing message by {context.Connection.Id}");

            await _recordService.AddAsync(context.Wallet, new BasicMessageRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = context.Connection.Id,
                Text = message.Content,
                SentTime = message.SentTime,
                Direction = MessageDirection.Incoming
            });

            return null;
        }
    }
}