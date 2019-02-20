using System;
using AgentFramework.Core.Decorators.Signature;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Dids;
using Hyperledger.Indy.DidApi;
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
        /// Gets or sets the connection object.
        /// </summary>
        /// <value>
        /// The connection object.
        /// </value>
        [JsonProperty("connection")]
        public Connection Connection { get; set; }

        /// <summary>
        /// Gets or sets the connection object.
        /// </summary>
        /// <value>
        /// The connection object.
        /// </value>
        [JsonProperty("connection~sig")]
        public SignatureDecorator ConnectionSig { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"Did={Connection?.Did}, ";
    }
}
