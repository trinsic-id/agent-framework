using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Proofs
{
    /// <summary>
    /// A proof request content message.
    /// </summary>
    public class ProofRequestMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ProofRequest;

        /// <summary>
        /// Gets or sets the proof request json.
        /// </summary>
        /// <value>
        /// The proof json.
        /// </value>
        public string ProofRequestJson { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"ProofRequestJson={(ProofRequestJson?.Length > 0 ? "[hidden]" : null)}";
    }
}
