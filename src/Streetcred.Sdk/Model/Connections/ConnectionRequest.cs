using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Connections
{
    /// <summary>
    /// Represents a connection request message.
    /// </summary>
    public class ConnectionRequest : IContentMessage
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
