﻿using System;
using System.Collections.Generic;
using AgentFramework.Core.Models.Payments;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records
{
    public class PaymentAddressRecord : RecordBase
    {
        public PaymentAddressRecord()
        {
            Id = Guid.NewGuid().ToString();
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

        public DateTime SourcesSyncedAt { get; set; }

        public IList<IndyPaymentInputSource> Sources { get; set; }
    }
}