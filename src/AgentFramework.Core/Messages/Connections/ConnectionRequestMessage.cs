using System;
using AgentFramework.Core.Models;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Connections
{
    /// <summary>
    /// Represents a connection request message.
    /// </summary>
    public class ConnectionRequestMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ConnectionRequest;

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
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>
        /// The image URL.
        /// </value>
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
