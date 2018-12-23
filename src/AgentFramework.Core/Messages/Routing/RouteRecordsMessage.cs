using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Routing
{
    /// <summary>
    /// Represents a routing records message
    /// </summary>
    public class RouteRecordsMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.RouteRecords;

        /// <summary>
        /// Gets or sets the recipient identifiers field.
        /// </summary>
        /// <value>
        /// The recipient identifiers associated to the connection.
        /// </value>
        [JsonProperty("recipient-identifiers")]
        public string[] RecipientIdentifiers { get; set; }
    }
}
