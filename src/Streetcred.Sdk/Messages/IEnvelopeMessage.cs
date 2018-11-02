using Newtonsoft.Json;
using Streetcred.Sdk.Messages.Converters;

namespace Streetcred.Sdk.Messages
{
    /// <summary>
    /// Represents an envelop message
    /// </summary>
    [JsonConverter(typeof(EnvelopeMessageConverter))]
    public interface IEnvelopeMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        string Type { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        string Content { get; set; }
    }
}