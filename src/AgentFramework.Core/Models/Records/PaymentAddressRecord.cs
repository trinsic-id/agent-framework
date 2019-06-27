using System;
using System.Collections.Generic;
using System.Linq;
using AgentFramework.Core.Models.Payments;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records
{
    public class PaymentAddressRecord : RecordBase
    {
        public PaymentAddressRecord()
        {
            Id = Guid.NewGuid().ToString();
            Sources = new List<IndyPaymentInputSource>();
        }

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

        [JsonIgnore]
        public ulong Balance =>
            Sources.Any()
            ? Sources.Select(x => x.Amount).Aggregate((x, y) => x + y)
            : 0;

        public DateTime SourcesSyncedAt { get; set; }

        public IList<IndyPaymentInputSource> Sources { get; set; }
    }
}
