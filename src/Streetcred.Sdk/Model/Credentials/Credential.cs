using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Credentials
{
    /// <summary>
    /// A credential content message.
    /// </summary>
    public class Credential : IContentMessage
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
