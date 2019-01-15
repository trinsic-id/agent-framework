using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models.Messaging;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers.Default
{
    public class DefaultCredentialHandler : IMessageHandler
    {
        private readonly ICredentialService _credentialService;

        public DefaultCredentialHandler(ICredentialService credentialService)
        {
            _credentialService = credentialService;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<string> SupportedMessageTypes => new[]
        {
            MessageTypes.CredentialOffer,
            MessageTypes.CredentialRequest,
            MessageTypes.Credential
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentMessage">The agent message.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task ProcessAsync(MessageContext agentMessage, ConnectionContext context)
        {
            switch (agentMessage.MessageType)
            {
                case MessageTypes.CredentialOffer:
                    var offer = agentMessage.GetMessage<CredentialOfferMessage>();
                    var credentialId =
                        await _credentialService.ProcessOfferAsync(context.Wallet, offer, context.Connection);
                    await _credentialService.AcceptOfferAsync(context.Wallet, context.Pool, credentialId);
                    return;

                case MessageTypes.CredentialRequest:
                    var request = agentMessage.GetMessage<CredentialRequestMessage>();
                    await _credentialService.ProcessCredentialRequestAsync(context.Wallet, request, context.Connection);
                    return;

                case MessageTypes.Credential:
                    var credential = agentMessage.GetMessage<CredentialMessage>();
                    await _credentialService.ProcessCredentialAsync(context.Pool, context.Wallet, credential,
                        context.Connection);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {agentMessage.MessageType}");
            }
        }
    }
}