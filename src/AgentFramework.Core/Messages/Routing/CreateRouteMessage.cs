using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Routing
{
    /// <summary>
    /// Represents a create route message
    /// </summary>
    public class CreateRouteMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.CreateRoute;

        /// <summary>
        /// Gets or sets the recipient identifier field.
        /// </summary>
        /// <value>
        /// The recipient identifier of the routing record to be created.
        /// </value>
        [JsonProperty("recipient-identifier")]
        public string RecipientIdentifier { get; set; }
    }
}
