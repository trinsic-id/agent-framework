using System.Collections.Generic;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Payments
{
    public class PaymentDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayItems")]
        public IList<PaymentItem> Items { get; set; }

        [JsonProperty("total")]
        public PaymentItem Total { get; set; }
    }
}
