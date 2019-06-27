using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Ledger
{
    public class ConstraintMetadata
    {
        [JsonProperty("fees")]
        public string Fee { get; set; }
    }
}
