using Newtonsoft.Json;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Messages.Connections
{
    /// <summary>
    /// Represents a connection request message.
    /// </summary>
    public class ConnectionRequestMessage : IContentMessage
    {
        /// <summary>
        /// Gets or sets the connection key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <inheritdoc />
        [JsonProperty("@type")]
        public string Type { get; set; }

        /// <inheritdoc />
        public string Content { get; set; }
    }
}
