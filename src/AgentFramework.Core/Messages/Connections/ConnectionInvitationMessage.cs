using System;
using System.Collections.Generic;
using AgentFramework.Core.Models;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Connections
{
    /// <summary>
    /// Represents an invitation message for establishing connection.
    /// </summary>
    public class ConnectionInvitationMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ConnectionInvitation;

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
        /// Gets or sets the service endpoint.
        /// </summary>
        /// <value>
        /// The service endpoint.
        /// </value>
        [JsonProperty("serviceEndpoint")]
        public string ServiceEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the routing keys.
        /// </summary>
        /// <value>
        /// The routing keys.
        /// </value>
        [JsonProperty("routing_keys")]
        public IList<string> RoutingKeys { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"Name={Name}, " +
            $"ImageUrl={ImageUrl}, " +
            $"RoutingKeys={string.Join(",", RoutingKeys)}, " +
            $"ConnectionKey={(ConnectionKey?.Length > 0 ? "[hidden]" : null)}, ";
    }
}
