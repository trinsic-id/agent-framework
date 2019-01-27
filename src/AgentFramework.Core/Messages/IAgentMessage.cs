using Newtonsoft.Json;

namespace AgentFramework.Core.Messages
{
    /// <summary>
    /// Represents a content message
    /// </summary>
    public interface IAgentMessage
    {
        /// <summary>
        /// Gets or sets the message id.
        /// </summary>
        /// <value>
        /// The message id.
        /// </value>
        [JsonProperty("@id")]
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        string Type { get; set; }
    }
}