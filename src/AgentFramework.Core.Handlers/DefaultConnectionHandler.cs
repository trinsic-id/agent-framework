using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Messaging;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Handlers
{
    public class DefaultConnectionHandler : IMessageHandler
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
        /// <param name="agentMessageContext">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {message.Type}</exception>
        public async Task ProcessAsync(MessageContext agentMessageContext)
        {
            switch (agentMessageContext.MessageType)
            {
                case MessageTypes.ConnectionInvitation:
                    var invitation = agentMessageContext.GetMessage<ConnectionInvitationMessage>();
                    await _connectionService.AcceptInvitationAsync(agentMessageContext.AgentContext.Wallet, invitation);
                    return;
                case MessageTypes.ConnectionRequest:
                    var request = agentMessageContext.GetMessage<ConnectionRequestMessage>();
                    var connectionId =
                        await _connectionService.ProcessRequestAsync(agentMessageContext.AgentContext.Wallet, request, agentMessageContext.Connection);
                    if (agentMessageContext.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                        await _connectionService.AcceptRequestAsync(agentMessageContext.AgentContext.Wallet, connectionId);
                    return;
                case MessageTypes.ConnectionResponse:
                    var response = agentMessageContext.GetMessage<ConnectionResponseMessage>();
                    await _connectionService.ProcessResponseAsync(agentMessageContext.AgentContext.Wallet, response, agentMessageContext.Connection);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {agentMessageContext.MessageType}");
            }
        }
    }
}