using System;
using System.Collections.Generic;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models.Messaging;

namespace WebAgent.Messages
{
    public class PrivateMessageHandler : IMessageHandler
    {
        public const string PrivateMessageType = "did:test:123456;spec/1.0/webagent/private_message";

        private readonly IWalletRecordService _recordService;

        public PrivateMessageHandler(IWalletRecordService recordService)
        {
            _recordService = recordService;
        }

        private Task ProcessPrivateMessage(PrivateMessage message, ConnectionContext context)
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

        public IEnumerable<string> SupportedMessageTypes => new[] { PrivateMessageType };

        public async Task ProcessAsync(MessageContext agentMessageContext, ConnectionContext connectionContext)
        {
            switch (agentMessageContext.MessageType)
            {
                case PrivateMessageType:
                    await ProcessPrivateMessage(agentMessageContext.GetMessage<PrivateMessage>(), connectionContext);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {agentMessageContext.MessageType}");
            }
        }
    }
}