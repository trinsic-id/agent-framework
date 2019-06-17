using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Payments
{
    public class PaymentMethod
    {
        [JsonProperty("supportedMethods")]
        public string SupportedMethods { get; set; }

        [JsonProperty("data")]
        public PaymentMethodData Data { get; set; }
    }
}
