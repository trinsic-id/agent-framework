using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Payments
{
    public class PaymentItem
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("amount")]
        public PaymentAmount Amount { get; set; }
    }
}
