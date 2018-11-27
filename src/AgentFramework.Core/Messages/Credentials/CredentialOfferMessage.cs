using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Credentials
{
    /// <summary>
    /// A credential offer content message.
    /// </summary>
    public class CredentialOfferMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.CredentialOffer;

        /// <summary>
        /// Gets or sets the offer json.
        /// </summary>
        /// <value>
        /// The offer json.
        /// </value>
        public string OfferJson { get; set; }
    }
}
