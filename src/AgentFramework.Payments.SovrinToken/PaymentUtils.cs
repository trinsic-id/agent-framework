using System;
using System.Collections.Generic;
using System.Linq;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Payments.SovrinToken
{
    internal static class PaymentUtils
    {
        internal static (IEnumerable<string> inputs, IEnumerable<IndyPaymentOutputSource> outputs, IEnumerable<IndyPaymentOutputSource> feesOutputs) ReconcilePaymentSources(PaymentAddressRecord addressRecord, PaymentRecord paymentRecord, ulong txnFee)
        {
            return ReconcilePaymentSources(addressRecord.Sources, paymentRecord.Address, paymentRecord.Amount, txnFee);
        }

        private static (IEnumerable<string> inputs, IEnumerable<IndyPaymentOutputSource> outputs, IEnumerable<IndyPaymentOutputSource> feesOutputs) ReconcilePaymentSources(IList<IndyPaymentInputSource> sources, string address, ulong amount, ulong txnFee)
        {
            if (amount == 0) throw new ArgumentOutOfRangeException(nameof(amount), "Cannot make a 0 payment");
            if (address == null) throw new ArgumentNullException(nameof(address), "Address must be specified");

            var selectedInputs = new List<IndyPaymentInputSource>();
            foreach (var input in sources)
            {
                selectedInputs.Add(input);
                if (Total(selectedInputs) + txnFee >= amount)
                {
                    break;
                }
            }

            if (!selectedInputs.Any()) throw new AgentFrameworkException(ErrorCode.PaymentInsufficientFunds, "Insufficient funds");

            return (selectedInputs.Select(x => x.Source), new[]
                {
                    new IndyPaymentOutputSource
                    {
                        Amount = amount,
                        Recipient = address
                    },
                    new IndyPaymentOutputSource
                    {
                        Recipient = selectedInputs.First().PaymentAddress,
                        Amount = Total(selectedInputs) - amount - txnFee
                    }
                }, new[]
                {
                   new IndyPaymentOutputSource
                    {
                        Recipient = selectedInputs.First().PaymentAddress,
                        Amount = Total(selectedInputs) - amount - txnFee
                    }
                });
        }

        private static ulong Total(IEnumerable<IndyPaymentInputSource> source)
        {
            return source.Any() ?
                source.Select(x => x.Amount)
                .Aggregate((x, y) => x + y) : 0;
        }
    }
}