using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

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

        Task<PaymentInfo> CreatePaymentInfoAsync(Pool pool, Wallet wallet, string transactionType);

        /// <summary>
        /// Gets the fees associated with a given transaction type
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        Task<ulong> GetTransactionFeeAsync(IAgentContext agentContext, string transactionType);
    }
}
