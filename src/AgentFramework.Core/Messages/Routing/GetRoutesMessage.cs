using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Routing
{
    /// <summary>
    /// Represents a get routing records message message
    /// </summary>
    public class GetRoutesMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.GetRoutes;
    }
}
