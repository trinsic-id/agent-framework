using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Messaging;

namespace AgentFramework.Core.Handlers
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
        /// <param name="agentMessageContext">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {messageType}</exception>
        public async Task ProcessAsync(MessageContext agentMessageContext)
        {
            switch (agentMessageContext.MessageType)
            {
                case MessageTypes.ProofRequest:
                    var request = agentMessageContext.GetMessage<ProofRequestMessage>();
                    await _proofService.ProcessProofRequestAsync(agentMessageContext.AgentContext.Wallet, request, agentMessageContext.Connection);
                    break;

                case MessageTypes.DisclosedProof:
                    var proof = agentMessageContext.GetMessage<ProofMessage>();
                    await _proofService.ProcessProofAsync(agentMessageContext.AgentContext.Wallet, proof, agentMessageContext.Connection);
                    break;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {agentMessageContext.MessageType}");
            }
        }
    }
}
