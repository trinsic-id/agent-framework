using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Ledger;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Indy = Hyperledger.Indy.PaymentsApi;
using IndyPayments = Hyperledger.Indy.PaymentsApi.Payments;

namespace AgentFramework.Payments.SovrinToken
{
    public class SovrinPaymentService : IPaymentService
    {
        private readonly IWalletRecordService recordService;
        private readonly IPoolService poolService;
        private readonly ILedgerService ledgerService;
        private readonly IProvisioningService provisioningService;
        private readonly ILogger<SovrinPaymentService> logger;
        private IDictionary<string, ulong> _transactionFees;

        public SovrinPaymentService(
            IWalletRecordService recordService,
            IPoolService poolService,
            ILedgerService ledgerService,
            IProvisioningService provisioningService,
            ILogger<SovrinPaymentService> logger)
        {
            this.recordService = recordService;
            this.poolService = poolService;
            this.ledgerService = ledgerService;
            this.provisioningService = provisioningService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<PaymentAddressRecord> CreatePaymentAddressAsync(IAgentContext agentContext, AddressOptions configuration = null)
        {
            var address = await IndyPayments.CreatePaymentAddressAsync(agentContext.Wallet, TokenConfiguration.MethodName,
                new { seed = configuration?.Seed }.ToJson());

            var addressRecord = new PaymentAddressRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Method = TokenConfiguration.MethodName,
                Address = address,
                SourcesSyncedAt = DateTime.MinValue
            };

            await recordService.AddAsync(agentContext.Wallet, addressRecord);

            return addressRecord;
        }

        public async Task<PaymentInfo> CreatePaymentInfoAsync(IAgentContext context, string transactionType, PaymentAddressRecord addressRecord = null)
        {
            var fees = await GetTransactionFeeAsync(context, transactionType);
            if (fees > 0)
            {
                if (addressRecord == null)
                {
                    var provisioning = await provisioningService.GetProvisioningAsync(context.Wallet);
                    if (provisioning.DefaultPaymentAddressId == null)
                    {
                        throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Default PaymentAddressRecord not found");
                    }
                    addressRecord = await recordService.GetAsync<PaymentAddressRecord>(context.Wallet, provisioning.DefaultPaymentAddressId);
                }

                return new PaymentInfo
                {
                    Amount = fees,
                    From = addressRecord,
                    PaymentMethod = "sov",
                    To = addressRecord.Address
                };
            }
            return null;
        }

        /// <inheritdoc />
        public async Task GetBalanceAsync(IAgentContext agentContext, PaymentAddressRecord paymentAddress = null)
        {
            if (paymentAddress == null)
            {
                var provisioning = await provisioningService.GetProvisioningAsync(agentContext.Wallet);
                if (provisioning.DefaultPaymentAddressId == null)
                {
                    throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Default PaymentAddressRecord not found");
                }

                paymentAddress = await recordService.GetAsync<PaymentAddressRecord>(agentContext.Wallet, provisioning.DefaultPaymentAddressId);
            }

            // Cache sources data in record for one hour
            var request = await IndyPayments.BuildGetPaymentSourcesAsync(agentContext.Wallet, null, paymentAddress.Address);
            var response = await Ledger.SubmitRequestAsync(await agentContext.Pool, request.Result);

            var sourcesJson = await Indy.Payments.ParseGetPaymentSourcesAsync(paymentAddress.Method, response);
            var sources = sourcesJson.ToObject<IList<IndyPaymentInputSource>>();
            paymentAddress.Sources = sources;
            paymentAddress.SourcesSyncedAt = DateTime.Now;

            await recordService.UpdateAsync(agentContext.Wallet, paymentAddress);
        }

        /// <inheritdoc />
        public async Task MakePaymentAsync(IAgentContext agentContext, PaymentRecord paymentRecord,
            PaymentAddressRecord addressFromRecord = null)
        {
            await paymentRecord.TriggerAsync(PaymentTrigger.ProcessPayment);
            if (paymentRecord.Address == null)
            {
                throw new AgentFrameworkException(ErrorCode.InvalidRecordData, "Payment record is missing an address");
            }

            var provisioning = await provisioningService.GetProvisioningAsync(agentContext.Wallet);
            if (addressFromRecord == null)
            {
                if (provisioning.DefaultPaymentAddressId == null)
                {
                    throw new AgentFrameworkException(ErrorCode.RecordNotFound,
                        "Default PaymentAddressRecord not found");
                }

                addressFromRecord = await recordService.GetAsync<PaymentAddressRecord>(
                    agentContext.Wallet, provisioning.DefaultPaymentAddressId);
            }

            await GetBalanceAsync(agentContext, addressFromRecord);
            if (addressFromRecord.Balance < paymentRecord.Amount)
            {
                throw new AgentFrameworkException(ErrorCode.PaymentInsufficientFunds,
                    "Address doesn't have enough funds to make this payment");
            }
            var txnFee = await GetTransactionFeeAsync(agentContext, TransactionTypes.XFER_PUBLIC);
            var paymentResult = await IndyPayments.BuildPaymentRequestAsync(
                wallet: agentContext.Wallet,
                submitterDid: null,
                inputsJson: addressFromRecord.Sources.Select(x => x.Source).ToJson(),
                outputsJson: new[]
                {
                    new IndyPaymentOutputSource
                    {
                        Amount = paymentRecord.Amount,
                        Recipient = paymentRecord.Address
                    },
                    new IndyPaymentOutputSource
                    {
                        Recipient = addressFromRecord.Address,
                        Amount = addressFromRecord.Balance - paymentRecord.Amount - txnFee
                    }
                }.ToJson(),
                extra: null);

            var response = await Ledger.SignAndSubmitRequestAsync(await agentContext.Pool, agentContext.Wallet,
                provisioning.Endpoint.Did, paymentResult.Result);

            var paymentResponse = await IndyPayments.ParsePaymentResponseAsync(TokenConfiguration.MethodName, response);
            var paymentOutputs = paymentResponse.ToObject<IList<IndyPaymentOutputSource>>();
            var paymentOutput = paymentOutputs.SingleOrDefault(x => x.Recipient == paymentRecord.Address);
            paymentRecord.ReceiptId = paymentOutput.Receipt;
            addressFromRecord.Sources = paymentOutputs
                .Where(x => x.Recipient == addressFromRecord.Address)
                .Select(x => new IndyPaymentInputSource
                {
                    Amount = x.Amount,
                    PaymentAddress = x.Recipient,
                    Source = x.Receipt
                })
                .ToList();

            await recordService.UpdateAsync(agentContext.Wallet, paymentRecord);
            await recordService.UpdateAsync(agentContext.Wallet, addressFromRecord);
        }

        public Task ProcessPaymentReceipt(IAgentContext agentContext, PaymentReceiptDecorator receiptDecorator, RecordBase recordBase = null)
        {
            throw new NotImplementedException();
        }

        public Task ProcessPaymentRequest(IAgentContext agentContext, PaymentRequestDecorator requestDecorator, RecordBase recordBase = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task SetDefaultPaymentAddressAsync(IAgentContext agentContext, PaymentAddressRecord addressRecord)
        {
            var provisioning = await provisioningService.GetProvisioningAsync(agentContext.Wallet);
            provisioning.DefaultPaymentAddressId = addressRecord.Id;

            await recordService.UpdateAsync(agentContext.Wallet, provisioning);
        }


        public async Task<ulong> GetTransactionFeeAsync(IAgentContext agentContext, string txnType)
        {
            var feeAliases = await GetTransactionFeesAsync(agentContext);
            var authRules = await LookupAuthorizationRulesAsync(await agentContext.Pool);

            // TODO: Add better selective logic that takes action and role into account
            // Ex: ADD action may have fees, but EDIT may not have any
            // Ex: Steward costs may be different than TrustAnchor costs, etc.

            var constraints = authRules
                .Where(x => x.TransactionType == txnType)
                .Select(x => x.Constraint)
                .Where(x => x.Metadata?.Fee != null);

            if (constraints.Count() > 1)
            {
                logger.LogWarning("Multiple fees found for {TransactionType} {Fees}", txnType, constraints.ToArray());
            }

            var constraint = constraints.FirstOrDefault();
            if (constraint != null && feeAliases.TryGetValue(constraint.Metadata.Fee, out var amount))
            {
                return amount;
            }

            constraints = authRules
                .Where(x => x.TransactionType == txnType)
                .Where(x => x.Constraint.Constraints != null)
                .SelectMany(x => x.Constraint.Constraints)
                .Where(x => x.Metadata?.Fee != null);

            if (constraints.Count() > 1)
            {
                logger.LogWarning("Multiple fees found for {TransactionType} {Fees}", txnType, constraints.ToArray());
            }

            constraint = constraints.FirstOrDefault();
            if (constraint != null && feeAliases.TryGetValue(constraint.Metadata.Fee, out amount))
            {
                return amount;
            }
            return 0;
        }

        /// <inheritdoc />
        public async Task<IList<AuthorizationRule>> LookupAuthorizationRulesAsync(Pool pool)
        {
            var req = await Ledger.BuildGetAuthRuleRequestAsync(null, null, null, null, null, null);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            EnsureSuccessResponse(res);

            var jobj = JObject.Parse(res);
            return jobj["result"]["data"].ToObject<IList<AuthorizationRule>>();
        }

        void EnsureSuccessResponse(string res)
        {
            var response = JObject.Parse(res);

            if (!response["op"].ToObject<string>().Equals("reply", StringComparison.OrdinalIgnoreCase))
                throw new AgentFrameworkException(ErrorCode.LedgerOperationRejected, "Ledger operation rejected");
        }

        private async Task<IDictionary<string, ulong>> GetTransactionFeesAsync(IAgentContext agentContext)
        {
            if (_transactionFees == null)
            {
                var feesRequest = await IndyPayments.BuildGetTxnFeesRequestAsync(agentContext.Wallet, null, TokenConfiguration.MethodName);
                var feesResponse = await Ledger.SubmitRequestAsync(await agentContext.Pool, feesRequest);

                var feesParsed = await IndyPayments.ParseGetTxnFeesResponseAsync(TokenConfiguration.MethodName, feesResponse);
                _transactionFees = feesParsed.ToObject<IDictionary<string, ulong>>();
            }
            return _transactionFees;
        }

        public async Task<PaymentRecord> AttachPaymentRequestAsync(IAgentContext context, AgentMessage agentMessage, PaymentDetails details, PaymentAddressRecord addressRecord)
        {
            // TODO: Add validation

            var paymentRecord = new PaymentRecord
            {
                Address = addressRecord.Address,
                Method = "sov",
                Amount = details.Total.Amount.Value,
                Details = details
            };
            await paymentRecord.TriggerAsync(PaymentTrigger.RequestSent);
            await recordService.AddAsync(context.Wallet, paymentRecord);

            details.Id = paymentRecord.Id;

            agentMessage.AddDecorator(new PaymentRequestDecorator
            {
                Method = new PaymentMethod
                {
                    SupportedMethods = "sov",
                    Data = new PaymentMethodData
                    {
                        PayeeId = addressRecord.Address,
                        SupportedNetworks = new[] { "Sovrin MainNet" }
                    }
                },
                Details = details
            }, "payment_request");

            return paymentRecord;
        }

        public async Task<PaymentAddressRecord> GetDefaultPaymentAddressAsync(IAgentContext agentContext)
        {
            var provisioning = await provisioningService.GetProvisioningAsync(agentContext.Wallet);
            if (provisioning.DefaultPaymentAddressId == null)
            {
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Default PaymentAddressRecord not found");
            }
            var paymentAddress = await recordService.GetAsync<PaymentAddressRecord>(agentContext.Wallet, provisioning.DefaultPaymentAddressId);
            return paymentAddress;
        }
    }
}
