using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Credentials
{
    /// <summary>
    /// A credential content message.
    /// </summary>
    public class CredentialMessage : IAgentMessage
    {
        /// <inheritdoc />
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
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
