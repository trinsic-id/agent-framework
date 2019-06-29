using System;
using System.Collections.Generic;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Models.Payments
{
    public class PaymentInfo
    {
        public PaymentAddressRecord From { get; set; }

        public string To { get; set; }

        public ulong Amount { get; set; }

        public string PaymentMethod { get; set; }
    }
}
