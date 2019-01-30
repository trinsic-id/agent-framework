using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Proofs;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class DefaultProofHandler : IMessageHandler
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
        /// <param name="messagePayload">The agent message agentContext.</param>
        /// <param name="agentContext"></param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task ProcessAsync(MessagePayload messagePayload, IAgentContext agentContext)
        {
            switch (messagePayload.GetMessageType())
            {
                case MessageTypes.ProofRequest:
                    var request = messagePayload.GetMessage<ProofRequestMessage>();
                    await _proofService.ProcessProofRequestAsync(agentContext.Wallet, request, agentContext.Connection);
                    break;

                case MessageTypes.DisclosedProof:
                    var proof = messagePayload.GetMessage<ProofMessage>();
                    await _proofService.ProcessProofAsync(agentContext.Wallet, proof, agentContext.Connection);
                    break;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messagePayload.GetMessageType()}");
            }
        }
    }
}
