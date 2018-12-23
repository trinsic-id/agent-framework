using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Did;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Connections
{
    /// <summary>
    /// Represents an invitation message for establishing connection.
    /// </summary>
    public class ConnectionInvitationMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>
        /// The endpoint.
        /// </value>
        [JsonProperty("endpoint")]
        public IDidService Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>
        /// The image URL.
        /// </value>
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the connection key.
        /// </summary>
        /// <value>
        /// The connection key.
        /// </value>
        [JsonProperty("connectionKey")]
        public string ConnectionKey { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ConnectionInvitation;
    }
}
