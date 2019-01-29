using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class DefaultCredentialHandler : IMessageHandler
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
        /// <param name="messagePayload">The agent message.</param>
        /// <param name="agentContext"></param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task ProcessAsync(MessagePayload messagePayload, AgentContext agentContext)
        {
            switch (messagePayload.GetMessageType())
            {
                case MessageTypes.CredentialOffer:
                    var offer = messagePayload.GetMessage<CredentialOfferMessage>();
                    var credentialId =
                        await _credentialService.ProcessOfferAsync(agentContext.Wallet, offer, agentContext.Connection);
                    await _credentialService.AcceptOfferAsync(agentContext.Wallet, agentContext.Pool, credentialId);
                    return;

                case MessageTypes.CredentialRequest:
                    var request = messagePayload.GetMessage<CredentialRequestMessage>();
                    await _credentialService.ProcessCredentialRequestAsync(agentContext.Wallet, request, agentContext.Connection);
                    return;

                case MessageTypes.Credential:
                    var credential = messagePayload.GetMessage<CredentialMessage>();
                    await _credentialService.ProcessCredentialAsync(agentContext.Pool, agentContext.Wallet, credential,
                        agentContext.Connection);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messagePayload.GetMessageType()}");
            }
        }
    }
}