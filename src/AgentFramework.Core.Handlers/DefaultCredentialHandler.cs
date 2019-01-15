using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Messaging;

namespace AgentFramework.Core.Handlers
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
        /// <param name="agentMessageContext">The agent message.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task ProcessAsync(MessageContext agentMessageContext)
        {
            switch (agentMessageContext.MessageType)
            {
                case MessageTypes.CredentialOffer:
                    var offer = agentMessageContext.GetMessage<CredentialOfferMessage>();
                    var credentialId =
                        await _credentialService.ProcessOfferAsync(agentMessageContext.AgentContext.Wallet, offer, agentMessageContext.Connection);
                    await _credentialService.AcceptOfferAsync(agentMessageContext.AgentContext.Wallet, agentMessageContext.AgentContext.Pool, credentialId);
                    return;

                case MessageTypes.CredentialRequest:
                    var request = agentMessageContext.GetMessage<CredentialRequestMessage>();
                    await _credentialService.ProcessCredentialRequestAsync(agentMessageContext.AgentContext.Wallet, request, agentMessageContext.Connection);
                    return;

                case MessageTypes.Credential:
                    var credential = agentMessageContext.GetMessage<CredentialMessage>();
                    await _credentialService.ProcessCredentialAsync(agentMessageContext.AgentContext.Pool, agentMessageContext.AgentContext.Wallet, credential,
                        agentMessageContext.Connection);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {agentMessageContext.MessageType}");
            }
        }
    }
}