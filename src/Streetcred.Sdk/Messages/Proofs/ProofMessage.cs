using Newtonsoft.Json;

namespace Streetcred.Sdk.Messages.Proofs
{
    /// <summary>
    /// A proof content message.
    /// </summary>
    public class ProofMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
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
