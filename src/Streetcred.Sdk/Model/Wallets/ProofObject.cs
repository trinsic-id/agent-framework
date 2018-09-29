using System.Collections.Generic;
using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Wallets
{
    public class ProofObject
    {
        [JsonProperty("identifiers")]
        public List<ProofIdentifier> Identifiers
        {
            get;
            set;
        }
    }

    public class ProofIdentifier
    {
        [JsonProperty("schema_id")]
        public string SchemaId { get; set; }

        [JsonProperty("cred_def_id")]
        public string CredentialDefintionId { get; set; }

        [JsonProperty("rev_reg_id")]
        public string RevocationRegistryId { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }
}
