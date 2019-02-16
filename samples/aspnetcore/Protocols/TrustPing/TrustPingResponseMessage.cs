using System;
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace WebAgent.Messages
{
    /// <summary>
    /// A ping response message.
    /// </summary>
    public class TrustPingResponseMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = CustomMessageTypes.TrustPingResponseMessageType;

        /// <summary>
        /// Gets or sets the comment of the message.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public string Comment { get; set; }
    }
}
