using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models.Messaging;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Handlers.Default
{
    public class DefaultProofHandler : IMessageHandler
    {
        private readonly IProofService _proofService;

        public DefaultProofHandler(IProofService proofService)
        {
            _proofService = proofService;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<string> SupportedMessageTypes => new[]
        {
            MessageTypes.ProofRequest,
            MessageTypes.DisclosedProof
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentMessageContext">The agent message context.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task ProcessAsync(MessageContext agentMessageContext, ConnectionContext context)
        {
            switch (agentMessageContext.MessageType)
            {
                case MessageTypes.ProofRequest:
                    var request = agentMessageContext.GetMessage<ProofRequestMessage>();
                    await _proofService.ProcessProofRequestAsync(context.Wallet, request, context.Connection);
                    break;

                case MessageTypes.DisclosedProof:
                    var proof = agentMessageContext.GetMessage<ProofMessage>();
                    await _proofService.ProcessProofAsync(context.Wallet, proof, context.Connection);
                    break;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {agentMessageContext.MessageType}");
            }
        }
    }
}
