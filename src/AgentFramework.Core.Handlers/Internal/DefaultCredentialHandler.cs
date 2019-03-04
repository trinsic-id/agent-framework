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
        private readonly IMessageService _messageService;

        /// <summary>Initializes a new instance of the <see cref="DefaultCredentialHandler"/> class.</summary>
        /// <param name="credentialService">The credential service.</param>
        /// <param name="messageService">The message service.</param>
        public DefaultCredentialHandler(
            ICredentialService credentialService,
            IMessageService messageService)
        {
            _credentialService = credentialService;
            _messageService = messageService;
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
        /// <param name="agentContext"></param>
        /// <param name="messagePayload">The agent message.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, MessagePayload messagePayload)
        {
            switch (messagePayload.GetMessageType())
            {
                case MessageTypes.CredentialOffer:
                {
                    var offer = messagePayload.GetMessage<CredentialOfferMessage>();
                    var credentialId = await _credentialService.ProcessOfferAsync(
                        agentContext, offer, agentContext.Connection);

                    var request = await _credentialService.AcceptOfferAsync(agentContext, credentialId);
                    await _messageService.SendToConnectionAsync(agentContext.Wallet, request, agentContext.Connection);
                    return null;
                }

                case MessageTypes.CredentialRequest:
                {
                    var request = messagePayload.GetMessage<CredentialRequestMessage>();

                    await _credentialService.ProcessCredentialRequestAsync(
                        agentContext, request, agentContext.Connection);
                    return null;
                }

                case MessageTypes.Credential:
                {
                    var credential = messagePayload.GetMessage<CredentialMessage>();
                    await _credentialService.ProcessCredentialAsync(
                        agentContext, credential, agentContext.Connection);
                    return null;
                }
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messagePayload.GetMessageType()}");
            }
        }
    }
}