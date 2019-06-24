using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
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

        public async Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, PaymentAddressConfiguration configuration = null)
        {
            var address = await Indy.Payments.CreatePaymentAddressAsync(agentContext.Wallet, TokenConfiguration.MethodName,
                new { seed = configuration?.AccountId }.ToJson());

            var addressRecord = new PaymentAddressRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Method = TokenConfiguration.MethodName,
                Address = address
            };

            await recordService.AddAsync(agentContext.Wallet, addressRecord);

            return addressRecord;
        }

        public async Task<PaymentAmount> GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress = null)
        {
            if (paymentAddress == null)
            {
                var provisioning = await provisioningService.GetProvisioningAsync(agentContext.Wallet);
                if (provisioning.DefaultPaymentAddressId == null)
                {
                    throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Default payment address record not found");
                }

                paymentAddress = await recordService.GetAsync<PaymentAddressRecord>(agentContext.Wallet, provisioning.DefaultPaymentAddressId);
            }

            // Cache sources data in record for one hour
            if (paymentAddress.SourcesSyncedAt > DateTime.Now.AddHours(-1))
            {
                var request = await Indy.Payments.BuildGetPaymentSourcesAsync(agentContext.Wallet, null, paymentAddress.Address);
                var response = await Ledger.SubmitRequestAsync(await agentContext.Pool, request.Result);

                var parsed = await Indy.Payments.ParseGetPaymentSourcesAsync(paymentAddress.Method, response);
                var paymentResult = parsed.ToObject<IList<IndyPaymentSource>>();
                paymentAddress.Sources = paymentResult;
                paymentAddress.SourcesSyncedAt = DateTime.Now;
                await recordService.UpdateAsync(agentContext.Wallet, paymentAddress);
            }
            return new PaymentAmount
            {
                Value = paymentAddress.Sources
                    .Select(x => x.Amount)
                    .Aggregate((x, y) => x + y)
                    .ToString(),
                Currency = TokenConfiguration.MethodName
            };
        }

        public Task MakePaymentAsync(IAgentContext agentContext, PaymentRecord paymentRecord, PaymentAddressRecord addressRecord = null)
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
