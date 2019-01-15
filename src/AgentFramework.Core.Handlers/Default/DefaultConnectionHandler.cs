using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Messaging;
using AgentFramework.Core.Utils;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Handlers.Default
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
        /// <param name="messageContext">The agent message context.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {message.Type}</exception>
        public async Task ProcessAsync(MessageContext messageContext, ConnectionContext context)
        {
            switch (messageContext.MessageType)
            {
                case MessageTypes.ConnectionInvitation:
                    var invitation = messageContext.GetMessage<ConnectionInvitationMessage>();
                    await _connectionService.AcceptInvitationAsync(context.Wallet, invitation);
                    return;
                case MessageTypes.ConnectionRequest:
                    var request = messageContext.GetMessage<ConnectionRequestMessage>();
                    var connectionId =
                        await _connectionService.ProcessRequestAsync(context.Wallet, request, context.Connection);
                    if (context.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                        await _connectionService.AcceptRequestAsync(context.Wallet, connectionId);
                    return;
                case MessageTypes.ConnectionResponse:
                    var response = messageContext.GetMessage<ConnectionResponseMessage>();
                    await _connectionService.ProcessResponseAsync(context.Wallet, response, context.Connection);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.MessageType}");
            }
        }
    }
}