using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Proofs;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Handlers.Default
{
    public class ProofHandler : IMessageHandler
    {
        private readonly IProofService _proofService;

        public ProofHandler(IProofService proofService)
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
                case MessageTypes.ProofRequest:
                    var request = item.ToObject<ProofRequestMessage>();
                    await _proofService.ProcessProofRequestAsync(context.Wallet, request, context.Connection);
                    break;

                case MessageTypes.DisclosedProof:
                    var proof = item.ToObject<ProofMessage>();
                    await _proofService.ProcessProofAsync(context.Wallet, proof, context.Connection);
                    break;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageType}");
            }
        }
    }
}
