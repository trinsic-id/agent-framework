using Newtonsoft.Json;

namespace Streetcred.Sdk.Messages.Connections
{
    /// <summary>
    /// Represents a connection response message
    /// </summary>
    public class ConnectionResponseMessage : IContentMessage
    {
        /// <summary>
        /// Gets or sets to.
        /// </summary>
        /// <value>
        /// To.
        /// </value>
        public string To { get; set; }
        
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public string Content { get; set; }
    }
}
