using Newtonsoft.Json;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Messages.Credentials
{
    /// <summary>
    /// A credential content message.
    /// </summary>
    public class CredentialMessage : IContentMessage
    {

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; } = MessageTypes.Credential;

        public string Content { get; set; }
    }
}
