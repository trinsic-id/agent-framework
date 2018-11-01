using Newtonsoft.Json;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Messages
{
    /// <summary>
    /// Represents a forward to key message envelope
    /// </summary>
    /// <seealso cref="IEnvelopeMessage" />
    public class ForwardToKeyEnvelopeMessage : IEnvelopeMessage
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