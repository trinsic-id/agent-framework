using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Proofs
{
    /// <summary>
    /// A proof request content message.
    /// </summary>
    public class ProofRequestMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ProofRequest;

        /// <summary>
        /// Gets or sets the proof request json.
        /// </summary>
        /// <value>
        /// The proof json.
        /// </value>
        public string ProofRequestJson { get; set; }
    }
}
