using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Streetcred.Sdk.Models.Proofs
{
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
}