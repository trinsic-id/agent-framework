using System;
using AgentFramework.Core.Models.Payments;
using Newtonsoft.Json;

namespace AgentFramework.Core.Decorators.Payments
{
    public class PaymentRequestDecorator
    {
        [JsonProperty("methodData")]
        public PaymentMethod Method { get; set; }

        [JsonProperty("details")]
        public PaymentDetails Details { get; set; }
    }
}
