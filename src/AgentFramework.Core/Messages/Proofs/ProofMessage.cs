using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Proofs
{
    /// <summary>
    /// A proof content message.
    /// </summary>
    public class ProofMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.DisclosedProof;

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
    }
}
