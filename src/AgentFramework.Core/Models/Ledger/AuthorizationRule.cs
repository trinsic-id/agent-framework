using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Ledger
{
    public class AuthorizationRule
    {
        [JsonProperty("auth_type")]
        public string TransactionType { get; set; }

        [JsonProperty("new_value")]
        public string NewValue { get; set; }

        [JsonProperty("old_value")]
        public string OldValue { get; set; }

        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("auth_action")]
        public string Action { get; set; }

        [JsonProperty("constraint")]
        public AuthorizationConstraint Constraint { get; set; }
    }
}
