using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
using AgentFramework.Payments.Abstractions;
using AgentFramework.Payments.Decorators;
using AgentFramework.Payments.Records;
using Hyperledger.Indy.LedgerApi;
using Indy = Hyperledger.Indy.PaymentsApi;

namespace AgentFramework.Payments.SovrinToken
{
    public class SovrinPaymentService : IPaymentService
    {
        private readonly IWalletRecordService recordService;
        private readonly IPoolService poolService;
        private readonly IProvisioningService provisioningService;

        public SovrinPaymentService(
            IWalletRecordService recordService,
            IPoolService poolService,
            IProvisioningService provisioningService)
        {
            this.recordService = recordService;
            this.poolService = poolService;
            this.provisioningService = provisioningService;
        }

        public async Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, PaymentAccountConfiguration configuration = null)
        {
            var address = await Indy.Payments.CreatePaymentAddressAsync(agentContext.Wallet, Configuration.MethodName,
                new { seed = configuration?.AccountId }.ToJson());

            var addressRecord = new PaymentAddressRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Method = Configuration.MethodName,
                Address = address
            };

            await recordService.AddAsync(agentContext.Wallet, addressRecord);

            return addressRecord;
        }

        public async Task<PaymentAmount> GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress)
        {
            var request = await Indy.Payments.BuildGetPaymentSourcesAsync(agentContext.Wallet, null, paymentAddress.Address);
            var response = await Ledger.SubmitRequestAsync(await agentContext.Pool, request.Result);

            var parsed = await Indy.Payments.ParseGetPaymentSourcesAsync(paymentAddress.Method, response);

            return new PaymentAmount { };
        }

        public Task MakePaymentAsync(IAgentContext agentContext, PaymentAddressRecord addressRecord, PaymentRecord paymentRecord)
        {
            throw new NotImplementedException();
        }

        public Task ProcessPaymentReceipt(IAgentContext agentContext, PaymentReceiptDecorator receiptDecorator, RecordBase recordBase = null)
        {
            throw new NotImplementedException();
        }

        public Task ProcessPaymentRequest(IAgentContext agentContext, PaymentRequestDecorator requestDecorator, RecordBase recordBase = null)
        {
            throw new NotImplementedException();
        }
    }
}
