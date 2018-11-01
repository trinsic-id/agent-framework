using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Streetcred.Sdk.Model.Wallets
{
    /// <summary>
    /// Proof request.
    /// </summary>
    public class ProofRequestObject
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the nonce.
        /// </summary>
        /// <value>The nonce.</value>
        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        /// <summary>
        /// Gets or sets the requested attributes.
        /// </summary>
        /// <value>The requested attributes.</value>
        [JsonProperty("requested_attributes")]
        public Dictionary<string, ProofAttributeInfo> RequestedAttributes { get; set; }

        /// <summary>
        /// Gets or sets the requested predicates.
        /// </summary>
        /// <value>The requested predicates.</value>
        [JsonProperty("requested_predicates", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ProofPredicateInfo> RequestedPredicates { get; set; } =
            new Dictionary<string, ProofPredicateInfo>();

        /// <summary>
        /// Gets or sets the non revoked.
        /// </summary>
        /// <value>The non revoked.</value>
        [JsonProperty("non_revoked", NullValueHandling = NullValueHandling.Ignore)]
        public RevocationInterval NonRevoked { get; set; }
    }

    /// <summary>
    /// Proof attribute info.
    /// </summary>
    public class ProofAttributeInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the restrictions.
        /// </summary>
        /// <example>
        /// filter_json: filter for credentials
        ///    {
        ///        "schema_id": string, (Optional)
        ///        "schema_issuer_did": string, (Optional)
        ///        "schema_name": string, (Optional)
        ///        "schema_version": string, (Optional)
        ///        "issuer_did": string, (Optional)
        ///        "cred_def_id": string, (Optional)
        ///    }
        /// </example>
        /// <value>The restrictions.</value>
        [JsonProperty("restrictions", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<AttributeFilter, string> Restrictions { get; set; }

        /// <summary>
        /// Gets or sets the non revoked.
        /// </summary>
        /// <value>The non revoked.</value>
        [JsonProperty("non_revoked", NullValueHandling = NullValueHandling.Ignore)]
        public RevocationInterval NonRevoked { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AttributeFilter
    {
        [EnumMember(Value = "schema_id")]
        SchemaId,
        [EnumMember(Value = "schema_issuer_did")]
        SchemaIssuerDid,
        [EnumMember(Value = "schema_name")]
        SchemaName,
        [EnumMember(Value = "schema_version")]
        SchemaVersion,
        [EnumMember(Value = "issuer_did")]
        IssuerDid,
        [EnumMember(Value = "cred_def_id")]
        CredentialDefinitionId
    }

    /// <summary>
    /// Proof revokation interval.
    /// </summary>
    public class RevocationInterval
    {
        /// <summary>
        /// Gets or sets from.
        /// </summary>
        /// <value>From.</value>
        [JsonProperty("from")]
        public uint From { get; set; }

        /// <summary>
        /// Gets or sets to.
        /// </summary>
        /// <value>To.</value>
        [JsonProperty("to")]
        public uint To { get; set; }
    }

    /// <inheritdoc />
    public class ProofPredicateInfo : ProofAttributeInfo
    {
        /// <summary>
        /// Gets or sets the type of the predicate.
        /// </summary>
        /// <value>The type of the predicate.</value>
        [JsonProperty("p_type")]
        public string PredicateType { get; set; }

        /// <summary>
        /// Gets or sets the predicate value.
        /// </summary>
        /// <value>The predicate value.</value>
        [JsonProperty("p_value")]
        public string PredicateValue { get; set; }
    }

    /// <summary>
    /// Requested credentials dto.
    /// </summary>
    public class RequestedCredentialsDto
    {
        /// <summary>
        /// Gets or sets the requested attributes.
        /// </summary>
        /// <value>The requested attributes.</value>
        [JsonProperty("requested_attributes")]
        public Dictionary<string, RequestedAttributeDto> RequestedAttributes { get; set; } =
            new Dictionary<string, RequestedAttributeDto>();

        /// <summary>
        /// Gets or sets the self attested attributes.
        /// </summary>
        /// <value>The self attested attributes.</value>
        [JsonProperty("self_attested_attributes")]
        public Dictionary<string, string> SelfAttestedAttributes { get; set; }
            = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the requested predicates.
        /// </summary>
        /// <value>The requested predicates.</value>
        [JsonProperty("requested_predicates")]
        public Dictionary<string, RequestedAttributeDto> RequestedPredicates { get; set; }
            = new Dictionary<string, RequestedAttributeDto>();


        /// <summary>
        /// Gets a collection of distinct credential identifiers found in this object.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<string> GetCredentialIdentifiers()
        {
            var credIds = new List<string>();
            credIds.AddRange(RequestedAttributes.Values.Select(x => x.CredentialId));
            credIds.AddRange(RequestedPredicates.Values.Select(x => x.CredentialId));
            return credIds.Distinct();
        }
    }

    /// <summary>
    /// Requested attribute dto.
    /// </summary>
    public class RequestedAttributeDto
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
        /// <see cref="RequestedAttributeDto"/> is revealed.
        /// </summary>
        /// <value><c>true</c> if revealed; otherwise, <c>false</c>.</value>
        [JsonProperty("revealed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Revealed { get; set; }

    }
}