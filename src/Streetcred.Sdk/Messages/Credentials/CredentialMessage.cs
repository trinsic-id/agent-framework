using Newtonsoft.Json;

namespace Streetcred.Sdk.Messages.Credentials
{
    /// <summary>
    /// A credential content message.
    /// </summary>
    public class CredentialMessage : IAgentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.Credential;

        /// <summary>
        /// Gets or sets the credential json.
        /// </summary>
        /// <value>
        /// The credential json.
        /// </value>
        public string CredentialJson { get; set; }

        /// <summary>
        /// Gets or sets the revocation registry identifier.
        /// </summary>
        /// <value>
        /// The revocation registry identifier.
        /// </value>
        public string RevocationRegistryId { get; set; }
    }
}
