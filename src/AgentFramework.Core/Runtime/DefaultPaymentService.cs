using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Runtime
{
    public class DefaultPaymentService : IPaymentService
    {
        public Task<PaymentRecord> AttachPaymentRequestAsync(IAgentContext context, AgentMessage agentMessage, PaymentDetails details, PaymentAddressRecord addressRecord)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, AddressOptions configuration = null)
        {
            throw new NotSupportedException();
        }

        public Task<PaymentInfo> CreatePaymentInfoAsync(IAgentContext context, string transactionType, PaymentAddressRecord addressRecord = null)
        {
            return Task.FromResult<PaymentInfo>(null);
        }

        public Task GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress = null)
        {
            throw new NotSupportedException();
        }

        public Task<ulong> GetTransactionFeeAsync(IAgentContext agentContext, string transactionType)
        {
            return Task.FromResult(0UL);
        }

        public Task MakePaymentAsync(IAgentContext agentContext, PaymentRecord paymentRecord, PaymentAddressRecord addressRecord = null)
        {
            throw new NotSupportedException();
        }

        public Task ProcessPaymentReceipt(IAgentContext agentContext, PaymentReceiptDecorator receiptDecorator, RecordBase recordBase = null)
        {
            throw new NotSupportedException();
        }

        public Task ProcessPaymentRequest(IAgentContext agentContext, PaymentRequestDecorator requestDecorator, RecordBase recordBase = null)
        {
            throw new NotSupportedException();
        }

        public Task SetDefaultPaymentAddressAsync(IAgentContext agentContext, PaymentAddressRecord addressRecord)
        {
            throw new NotSupportedException();
        }
    }
}
