using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AgentFramework.Core.Models.Proofs
{
    /// <summary>Attribute filter</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AttributeFilter
    {
        /// <summary>The schema identifier</summary>
        [EnumMember(Value = "schema_id")]
        SchemaId,
        /// <summary>The schema issuer did</summary>
        [EnumMember(Value = "schema_issuer_did")]
        SchemaIssuerDid,
        /// <summary>The schema name</summary>
        [EnumMember(Value = "schema_name")]
        SchemaName,
        /// <summary>The schema version</summary>
        [EnumMember(Value = "schema_version")]
        SchemaVersion,
        /// <summary>The issuer did</summary>
        [EnumMember(Value = "issuer_did")]
        IssuerDid,
        /// <summary>The credential definition identifier</summary>
        [EnumMember(Value = "cred_def_id")]
        CredentialDefinitionId
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
        /// <code>
        /// filter_json: filter for credentials
        ///    {
        ///        "schema_id": string, (Optional)
        ///        "schema_issuer_did": string, (Optional)
        ///        "schema_name": string, (Optional)
        ///        "schema_version": string, (Optional)
        ///        "issuer_did": string, (Optional)
        ///        "cred_def_id": string, (Optional)
        ///    }
        /// </code>
        /// </summary>
        /// <value>The restrictions.</value>
        [JsonProperty("restrictions", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<AttributeFilter, string> Restrictions { get; set; }

        /// <summary>
        /// Gets or sets the non revoked interval.
        /// </summary>
        /// <value>
        /// The non revoked.
        /// </value>
        [JsonProperty("non_revoked", NullValueHandling = NullValueHandling.Ignore)]
        public RevocationInterval NonRevoked { get; set; }
    }
}