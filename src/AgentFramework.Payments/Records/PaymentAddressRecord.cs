using System;
using AgentFramework.Core.Models.Records;
using Newtonsoft.Json;

namespace AgentFramework.Payments.Records
{
    public class PaymentAddressRecord : RecordBase
    {
        public override string TypeName => "AF.PaymentAddress";

        [JsonIgnore]
        public string Address
        {
            get => Get();
            set => Set(value);
        }

        [JsonIgnore]
        public string Method
        {
            get => Get();
            set => Set(value);
        }
    }
}
