using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models.Ledger;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using IndyPayments = Hyperledger.Indy.PaymentsApi.Payments;

namespace AgentFramework.Payments.SovrinToken
{
    public class SovrinFeesService : IFeesService
    {
        private readonly ILogger<SovrinFeesService> logger;
        private IDictionary<string, ulong> _transactionFees;

        public SovrinFeesService(ILogger<SovrinFeesService> logger)
        {
            this.logger = logger;
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
    }
}
