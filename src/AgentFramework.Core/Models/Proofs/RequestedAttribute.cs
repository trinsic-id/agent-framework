using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Proofs
{
    /// <summary>
    /// Requested attribute dto.
    /// </summary>
    public class RequestedAttribute
    {
        /// <summary>
        /// Gets or sets the credential identifier.
        /// </summary>
        /// <value>The credential identifier.</value>
        [JsonProperty("cred_id")]
        public string CredentialId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public long? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this
        ////// <see cref="RequestedAttribute"/> is revealed.    /// </summary>
        /// <value><c>true</c> if revealed; otherwise, <c>false</c>.</value>
        [JsonProperty("revealed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Revealed { get; set; }

    }
}