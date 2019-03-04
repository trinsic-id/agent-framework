using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Proofs
{
    /// <summary>
    /// A proof request content message.
    /// </summary>
    public class ProofRequestMessage : AgentMessage
    {
        /// <inheritdoc />
        public ProofRequestMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.ProofRequest;
        }
        
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
