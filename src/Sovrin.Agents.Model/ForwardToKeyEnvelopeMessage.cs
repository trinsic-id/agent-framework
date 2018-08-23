using Newtonsoft.Json;

namespace Sovrin.Agents.Model
{
    /// <summary>
    /// Represents a forward to key message envelope
    /// </summary>
    /// <seealso cref="IEnvelopeMessage" />
    public class ForwardToKeyEnvelopeMessage : IEnvelopeMessage
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ForwardToKey;

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}