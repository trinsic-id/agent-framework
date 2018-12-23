using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Routing
{
    /// <summary>
    /// Represents a forwarding to multiple recipients message
    /// </summary>
    public class ForwardMultipleMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ForwardMultiple;

        /// <summary>
        /// Gets or sets the to or recipient field.
        /// </summary>
        /// <value>
        /// The to or recipient of the message.
        /// </value>
        [JsonProperty("to")]
        public string[] To { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
