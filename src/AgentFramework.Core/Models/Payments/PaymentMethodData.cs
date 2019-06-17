using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Payments
{
    public class PaymentMethodData
    {
        [JsonProperty("supportedNetworks")]
        public string[] SupportedNetworks { get; set; }

        [JsonProperty("payeeId")]
        public string PayeeId { get; set; }
    }
}
