using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers.Default
{
    public class CredentialHandler : IMessageHandler
    {
        private readonly ICredentialService _credentialService;

        public CredentialHandler(ICredentialService credentialService)
        {
            _credentialService = credentialService;
        }

        public IEnumerable<string> SupportedMessageTypes => new[]
        {
            MessageTypes.CredentialOffer,
            MessageTypes.CredentialRequest,
            MessageTypes.Credential
        };

        public async Task OnMessageAsync(string agentMessage, AgentContext context)
        {
            var message = JsonConvert.DeserializeObject<IAgentMessage>(agentMessage);

            switch (message)
            {
                case CredentialOfferMessage offer:
                    var credentialId =
                        await _credentialService.ProcessOfferAsync(context.Wallet, offer, context.Connection);
                    await _credentialService.AcceptOfferAsync(context.Wallet, context.Pool, credentialId);
                    return;

                case CredentialRequestMessage request:
                    await _credentialService.ProcessCredentialRequestAsync(context.Wallet, request, context.Connection);
                    return;

                case CredentialMessage credential:
                    await _credentialService.ProcessCredentialAsync(context.Pool, context.Wallet, credential,
                        context.Connection);
                    return;
            }

            throw new AgentFrameworkException(ErrorCode.InvalidMessage, $"Unsupported message type {message.Type}");
        }
    }
}