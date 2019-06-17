using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Payments
{
    public class PaymentAmount
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
