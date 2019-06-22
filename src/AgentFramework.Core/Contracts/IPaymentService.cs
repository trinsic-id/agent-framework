using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Contracts
{
    public interface IPaymentService
    {
        Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, PaymentAccountConfiguration configuration = null);

        Task MakePaymentAsync(IAgentContext agentContext, PaymentAddressRecord addressRecord, PaymentRecord paymentRecord);

        Task<PaymentAmount> GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress);

        Task ProcessPaymentRequest(IAgentContext agentContext, PaymentRequestDecorator requestDecorator, RecordBase recordBase = null);

        Task ProcessPaymentReceipt(IAgentContext agentContext, PaymentReceiptDecorator receiptDecorator, RecordBase recordBase = null);
    }
}
