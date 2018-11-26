using Newtonsoft.Json;
using Streetcred.Sdk.Models;

namespace Streetcred.Sdk.Messages.Connections
{
    /// <summary>
    /// Represents a connection request message.
    /// </summary>
    public class ConnectionRequestMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the did.
        /// </summary>
        /// <value>
        /// The did.
        /// </value>
        [JsonProperty("did")]
        public string Did { get; set; }

        /// <summary>
        /// Gets or sets the verkey.
        /// </summary>
        /// <value>
        /// The verkey.
        /// </value>
        [JsonProperty("verkey")]
        public string Verkey { get; set; }

        /// <summary>
        /// Gets or sets the public endpoint.
        /// </summary>
        /// <value>
        /// The public endpoint.
        /// </value>
        [JsonProperty("endpoint")]
        public AgentEndpoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ConnectionRequest;
    }
}
