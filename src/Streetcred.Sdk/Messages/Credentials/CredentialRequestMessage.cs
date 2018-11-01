using Newtonsoft.Json;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Messages.Credentials
{
    /// <summary>
    /// A credential request content message.
    /// </summary>
    public class CredentialRequestMessage : IContentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.CredentialRequest;

        public string Content { get; set; }
    }
}
