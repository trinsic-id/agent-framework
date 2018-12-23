using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Routing
{
    /// <summary>
    /// Represents a delete route message
    /// </summary>
    public class DeleteRouteMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.DeleteRoute;

        /// <summary>
        /// Gets or sets the recipient identifier field.
        /// </summary>
        /// <value>
        /// The recipient identifier of the routing record to be deleted.
        /// </value>
        [JsonProperty("recipient-identifier")]
        public string RecipientIdentifier { get; set; }
    }
}
