using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Connections
{
    /// <summary>
    /// Represents connection details
    /// </summary>
    public class ConnectionDetails
    {
        /// <summary>
        /// Gets or sets the did.
        /// </summary>
        /// <value>
        /// The did.
        /// </value>
        [JsonProperty("did")]
        public string Did { get; set; }

        /// <summary>
        /// Gets or sets the verkey.
        /// </summary>
        /// <value>
        /// The verkey.
        /// </value>
        [JsonProperty("verkey")]
        public string Verkey { get; set; }

        /// <summary>
        /// Gets or sets the public endpoint.
        /// </summary>
        /// <value>
        /// The public endpoint.
        /// </value>
        [JsonProperty("endpoint")]
        public AgentEndpoint Endpoint { get; set; }
    }
}