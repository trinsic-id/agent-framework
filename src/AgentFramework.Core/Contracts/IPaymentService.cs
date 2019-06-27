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

        Task<ulong> GetTransactionFeeAsync(IAgentContext agentContext, string transactionType);

        /// <summary>
        /// Gets a list of fees aliases set for this ledger.
        /// The fees aliases will be used in the authorization rules metadata.
        /// </summary>
        /// <param name="agentContext"></param>
        /// <returns></returns>
        Task<IDictionary<string, ulong>> GetTransactionFeesAsync(IAgentContext agentContext);

        Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, PaymentAddressConfiguration configuration = null);

        Task MakePaymentAsync(IAgentContext agentContext, PaymentRecord paymentRecord, PaymentAddressRecord addressRecord = null);

        Task GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress = null);

        Task ProcessPaymentRequest(IAgentContext agentContext, PaymentRequestDecorator requestDecorator, RecordBase recordBase = null);

        Task ProcessPaymentReceipt(IAgentContext agentContext, PaymentReceiptDecorator receiptDecorator, RecordBase recordBase = null);
    }
}
