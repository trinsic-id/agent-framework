using System;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Payments
{
    public static class AgentMessageExtensions
    {
        public static (AgentMessage, PaymentRecord) AddPaymentRequest(this AgentMessage agentMessage, PaymentMethod method, PaymentDetails details, PaymentAddressRecord addressRecord)
        {
            agentMessage.AddDecorator(new PaymentRequestDecorator
            {
                Method = method,
                Details = details
            }, "payment_request");

            return (agentMessage, new PaymentRecord()
            {
            });
        }

        public static AgentMessage AddPaymentReceipt(this AgentMessage agentMessage, PaymentRecord paymentRecord)
        {
            if (paymentRecord.State != PaymentState.Paid)
            {
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState, "Payment state must be 'Paid'");
            }
            agentMessage.AddDecorator(new PaymentRequestDecorator(), "payment_receipt");

            return agentMessage;
        }
    }
}
