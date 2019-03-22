using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Proofs
{
    /// <summary>
    /// A proof content message.
    /// </summary>
    public class ProofMessage : AgentMessage
    {
        /// <inheritdoc />
        public ProofMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.DisclosedProof;
        }
        
        /// <summary>
        /// Gets or sets the proof json.
        /// </summary>
        /// <value>
        /// The proof json.
        /// </value>
        public string ProofJson { get; set; }

        /// <summary>
        /// Gets or sets the proof request nonce.
        /// </summary>
        /// <value>
        /// The request nonce.
        /// </value>
        public string RequestNonce { get; set; }
        
        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"ProofJson={(ProofJson?.Length > 0 ? "[hidden]" : null)}, " +
            $"RequestNonce={RequestNonce}";
    }
}
