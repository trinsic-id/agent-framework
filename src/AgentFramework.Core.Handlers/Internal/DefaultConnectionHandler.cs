using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class DefaultConnectionHandler : IMessageHandler
    {
        private readonly IConnectionService _connectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConnectionHandler"/> class.
        /// </summary>
        /// <param name="connectionService">The connection service.</param>
        public DefaultConnectionHandler(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

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
        public async Task ProcessAsync(IAgentContext agentContext, MessagePayload messagePayload)
        {
            switch (messagePayload.GetMessageType())
            {
                case MessageTypes.ConnectionInvitation:
                    var invitation = messagePayload.GetMessage<ConnectionInvitationMessage>();
                    await _connectionService.AcceptInvitationAsync(agentContext, invitation);
                    return;

                case MessageTypes.ConnectionRequest:
                    var request = messagePayload.GetMessage<ConnectionRequestMessage>();
                    var connectionId = await _connectionService.ProcessRequestAsync(agentContext, request);
                    // Auto accept connection if set during invitation
                    if (agentContext.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                    {
                        // Add a response message to be processed by the handler pipeline
                        if (agentContext is AgentContext context)
                            context.AddNext(new OutgoingMessage 
                                {
                                    OutboundMessage = new MessagePayload((await _connectionService.AcceptRequestAsync(agentContext, connectionId))),
                                    InboundMessage = messagePayload
                                }.AsMessagePayload());
                    }

                    return;

                case MessageTypes.ConnectionResponse:
                    var response = messagePayload.GetMessage<ConnectionResponseMessage>();
                    await _connectionService.ProcessResponseAsync(agentContext, response);
                    return;

                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messagePayload.GetMessageType()}");
            }
        }
    }
}