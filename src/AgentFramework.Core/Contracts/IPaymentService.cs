using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Contracts
{
    public interface IPaymentService
    {
        Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, PaymentAccountConfiguration configuration);

        Task MakePaymentAsync(IAgentContext agentContext, PaymentAddressRecord addressRecord, PaymentRecord paymentRecord);

        Task<PaymentAmount> GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress);
    }
}
