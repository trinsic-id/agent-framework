using System;
using Newtonsoft.Json;

namespace AgentFramework.Payments.SovrinToken.Models
{
    public class IndyPaymentResult
    {
        [JsonProperty("paymentAddress")]
        public string PaymentAddress { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("amount")]
        public ulong Amount { get; set; }

        [JsonProperty("extra")]
        public string Extra { get; set; }
    }
}
