using Newtonsoft.Json;

namespace Streetcred.Sdk.Messages
{
    /// <summary>
    /// Represents a content message
    /// </summary>
    [JsonConverter(typeof(AgentMessageConverter))]
    public interface IAgentMessage
    {
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