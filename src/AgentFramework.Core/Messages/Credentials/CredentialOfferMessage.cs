using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Credentials
{
    /// <summary>
    /// A credential offer content message.
    /// </summary>
    public class CredentialOfferMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.CredentialOffer;

        /// <summary>
        /// Gets or sets the offer json.
        /// </summary>
        /// <value>
        /// The offer json.
        /// </value>
        public string OfferJson { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"OfferJson={(OfferJson?.Length > 0 ? "[hidden]" : null)}";
    }
}
