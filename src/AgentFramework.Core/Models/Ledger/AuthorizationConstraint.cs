using System.Collections.Generic;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Ledger
{
    public class AuthorizationConstraint
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("need_to_be_owner")]
        public bool MustBeOwner { get; set; }

        [JsonProperty("sig_count")]
        public int SignatureCount { get; set; }

        [JsonProperty("metadata")]
        public ConstraintMetadata Metadata { get; set; }

        [JsonProperty("constraint_id")]
        public string ConstraintId { get; set; }

        [JsonProperty("auth_constraints")]
        public IList<AuthorizationConstraint> Constraints { get; set; }
    }
}
