using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
using AgentFramework.Payments.Decorators;
using AgentFramework.Payments.Records;

namespace AgentFramework.Payments.Abstractions
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
