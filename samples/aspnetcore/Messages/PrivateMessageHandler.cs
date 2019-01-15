using System;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using System.Threading.Tasks;

namespace WebAgent.Messages
{
    public class PrivateMessageHandler : MessageHandlerBase<PrivateMessage>
    {
        private readonly IWalletRecordService _recordService;

        public PrivateMessageHandler(IWalletRecordService recordService)
        {
            _recordService = recordService;
        }

        protected override Task ProcessAsync(PrivateMessage message, ConnectionContext context)
        {
            Console.WriteLine($"Processing message by {context.Connection.Id}");

            return _recordService.AddAsync(context.Wallet, new PrivateMessageRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = context.Connection.Id,
                Text = message.Text,
                Direction = MessageDirection.Incoming
            });
        }
    }
}