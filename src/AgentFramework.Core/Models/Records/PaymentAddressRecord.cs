using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records
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
