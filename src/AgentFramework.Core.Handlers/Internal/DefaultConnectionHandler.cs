using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class DefaultConnectionHandler : IMessageHandler
    {
        private readonly IConnectionService _connectionService;
        private readonly IMessageService _messageService;

        /// <summary>Initializes a new instance of the <see cref="DefaultConnectionHandler"/> class.</summary>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="messageService">The message service.</param>
        public DefaultConnectionHandler(
            IConnectionService connectionService,
            IMessageService messageService)
        {
            _connectionService = connectionService;
            _messageService = messageService;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<string> SupportedMessageTypes => new[]
        {
            MessageTypes.ConnectionInvitation,
            MessageTypes.ConnectionRequest,
            MessageTypes.ConnectionResponse
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messagePayload">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {message.Type}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, MessagePayload messagePayload)
        {
            switch (messagePayload.GetMessageType())
            {
                case MessageTypes.ConnectionInvitation:
                    var invitation = messagePayload.GetMessage<ConnectionInvitationMessage>();
                    await _connectionService.AcceptInvitationAsync(agentContext, invitation);
                    return null;

                case MessageTypes.ConnectionRequest:
                {
                    var request = messagePayload.GetMessage<ConnectionRequestMessage>();
                    var connectionId = await _connectionService.ProcessRequestAsync(agentContext, request);
                    // Auto accept connection if set during invitation
                    if (agentContext.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                    {
                        try
                        {
                            var message = await _connectionService.AcceptRequestAsync(agentContext, connectionId);
                            message.ThreadFrom(request);
                            await _messageService.SendToConnectionAsync(agentContext.Wallet, message, agentContext.Connection);
                            }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    return null;
                }

                case MessageTypes.ConnectionResponse:
                {
                    var response = messagePayload.GetMessage<ConnectionResponseMessage>();
                    await _connectionService.ProcessResponseAsync(agentContext, response);
                    return null;
                }
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messagePayload.GetMessageType()}");
            }
        }
    }
}