using Newtonsoft.Json;

namespace Streetcred.Sdk.Messages
{
    /// <summary>
    /// Represents a forwarding message envelope
    /// </summary>
    /// <seealso cref="IEnvelopeMessage" />
    public class ForwardEnvelopeMessage : IEnvelopeMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; }

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
