using System;
using AgentFramework.Core.Models;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Connections
{
    /// <summary>
    /// Represents a connection response message
    /// </summary>
    public class ConnectionResponseMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.ConnectionResponse;

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
    }
}
