using AgentFramework.Core.Models.Payments;
using Newtonsoft.Json;

namespace AgentFramework.Core.Decorators.Payments
{
    public class PaymentReceiptDecorator
    {
        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("selected_method")]
        public string SelectedMethod { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("payeeId")]
        public string PayeeId { get; set; }

        [JsonProperty("amount")]
        public PaymentAmount Amount { get; set; }
    }
}
