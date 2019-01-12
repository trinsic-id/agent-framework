using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Handlers.Default
{
    public class CredentialHandler : IMessageHandler
    {
        private readonly ICredentialService _credentialService;

        public CredentialHandler(ICredentialService credentialService)
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
        public async Task ProcessAsync(string agentMessage, ConnectionContext context)
        {
            var item = JObject.Parse(agentMessage);
            var messageType = item["@type"].ToObject<string>();

            switch (messageType)
            {
                case MessageTypes.CredentialOffer:
                    var offer = item.ToObject<CredentialOfferMessage>();
                    var credentialId =
                        await _credentialService.ProcessOfferAsync(context.Wallet, offer, context.Connection);
                    await _credentialService.AcceptOfferAsync(context.Wallet, context.Pool, credentialId);
                    return;

                case MessageTypes.CredentialRequest:
                    var request = item.ToObject<CredentialRequestMessage>();
                    await _credentialService.ProcessCredentialRequestAsync(context.Wallet, request, context.Connection);
                    return;

                case MessageTypes.Credential:
                    var credential = item.ToObject<CredentialMessage>();
                    await _credentialService.ProcessCredentialAsync(context.Pool, context.Wallet, credential,
                        context.Connection);
                    return;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageType}");
            }
        }
    }
}