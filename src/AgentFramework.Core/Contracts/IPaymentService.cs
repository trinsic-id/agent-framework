using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Contracts
{
    public interface IPaymentService
    {
        Task SetDefaultPaymentAddressAsync(IAgentContext agentContext, PaymentAddressRecord addressRecord);

        Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, PaymentAddressConfiguration configuration = null);

        Task MakePaymentAsync(IAgentContext agentContext, PaymentRecord paymentRecord, PaymentAddressRecord addressRecord = null);

        Task GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress = null);

        Task ProcessPaymentRequest(IAgentContext agentContext, PaymentRequestDecorator requestDecorator, RecordBase recordBase = null);

        Task ProcessPaymentReceipt(IAgentContext agentContext, PaymentReceiptDecorator receiptDecorator, RecordBase recordBase = null);
    }
}
