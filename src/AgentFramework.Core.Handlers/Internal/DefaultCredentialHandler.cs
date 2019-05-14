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
        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            new MessageType(MessageTypes.CredentialOffer),
            new MessageType(MessageTypes.CredentialRequest),
            new MessageType(MessageTypes.Credential)
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messageContext">The agent message.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, MessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case MessageTypes.CredentialOffer:
                {
                    var offer = messageContext.GetMessage<CredentialOfferMessage>();
                    await _credentialService.ProcessOfferAsync(
                        agentContext, offer, messageContext.Connection);

                    return null;
                }

                case MessageTypes.CredentialRequest:
                {
                    var request = messageContext.GetMessage<CredentialRequestMessage>();
                    await _credentialService.ProcessCredentialRequestAsync(
                        agentContext, request, messageContext.Connection);

                    return null;
                }

                case MessageTypes.Credential:
                {
                    var credential = messageContext.GetMessage<CredentialMessage>();
                    await _credentialService.ProcessCredentialAsync(
                        agentContext, credential, messageContext.Connection);

                    return null;
                }
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}