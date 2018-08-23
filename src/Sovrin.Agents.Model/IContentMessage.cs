using Newtonsoft.Json;
using Sovrin.Agents.Model.Converters;

namespace Sovrin.Agents.Model
{
    /// <summary>
    /// Represents a content message
    /// </summary>
    [JsonConverter(typeof(ContentMessageConverter))]
    public interface IContentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        string Type { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        string Content { get; set; }
    }
}