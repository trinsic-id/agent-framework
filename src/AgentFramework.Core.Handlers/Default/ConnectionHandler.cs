using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Utils;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers.Default
{
    public class ConnectionHandler : IMessageHandler
    {
        private readonly IConnectionService _connectionService;

        public ConnectionHandler(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public IEnumerable<string> SupportedMessageTypes => new[]
        {
            MessageTypes.ConnectionInvitation,
            MessageTypes.ConnectionRequest,
            MessageTypes.ConnectionResponse
        };

        public async Task OnMessageAsync(string agentMessage, AgentContext context)
        {
            var message = JsonConvert.DeserializeObject<IAgentMessage>(agentMessage);

            switch (message)
            {
                case ConnectionInvitationMessage invitation:
                    await _connectionService.AcceptInvitationAsync(context.Wallet, invitation);
                    return;
                case ConnectionRequestMessage request:
                    var connectionId =
                        await _connectionService.ProcessRequestAsync(context.Wallet, request, context.Connection);
                    if (context.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                        await _connectionService.AcceptRequestAsync(context.Wallet, connectionId);
                    return;
                case ConnectionResponseMessage response:
                    await _connectionService.ProcessResponseAsync(context.Wallet, response, context.Connection);
                    return;
            }

            throw new AgentFrameworkException(ErrorCode.InvalidMessage, $"Unsupported message type {message.Type}");
        }
    }
}