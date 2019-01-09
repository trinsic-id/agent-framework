using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultLedgerService : ILedgerService
    {
        /// <inheritdoc />
        public virtual async Task<ParseResponseResult> LookupDefinitionAsync(Pool pool,
            string definitionId)
        {
            var req = await Ledger.BuildGetCredDefRequestAsync(null, definitionId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return await Ledger.ParseGetCredDefResponseAsync(res);
        }

        /// <inheritdoc />
        public virtual async Task<ParseResponseResult> LookupRevocationRegistryDefinitionAsync(Pool pool,
            string registryId)
        {
            var req = await Ledger.BuildGetRevocRegDefRequestAsync(null, registryId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return await Ledger.ParseGetRevocRegDefResponseAsync(res);
        }

        /// <inheritdoc />
        public virtual async Task<ParseResponseResult> LookupSchemaAsync(Pool pool, string schemaId)
        {
            var req = await Ledger.BuildGetSchemaRequestAsync(null, schemaId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return await Ledger.ParseGetSchemaResponseAsync(res);
        }

        /// <inheritdoc />
        public virtual async Task<ParseRegistryResponseResult> LookupRevocationRegistryDeltaAsync(Pool pool, string revocationRegistryId,
             long from, long to)
        {
            var req = await Ledger.BuildGetRevocRegDeltaRequestAsync(null, revocationRegistryId, from, to);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            EnsureSuccessResponse(res);

            return await Ledger.ParseGetRevocRegDeltaResponseAsync(res);
        }

        /// <inheritdoc />
        public virtual async Task<ParseRegistryResponseResult> LookupRevocationRegistryAsync(Pool pool, string revocationRegistryId,
             long timestamp)
        {
            var req = await Ledger.BuildGetRevocRegRequestAsync(null, revocationRegistryId, timestamp);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            EnsureSuccessResponse(res);

            return await Ledger.ParseGetRevocRegResponseAsync(res);
        }
        
        /// <inheritdoc />
        public virtual async Task RegisterSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string schemaJson)
        {
            var req = await Ledger.BuildSchemaRequestAsync(issuerDid, schemaJson);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, issuerDid, req);

            EnsureSuccessResponse(res);
        }
        
        /// <inheritdoc />
        public virtual async Task RegisterCredentialDefinitionAsync(Wallet wallet, Pool pool, string submitterDid, string data)
        {
            var req = await Ledger.BuildCredDefRequestAsync(submitterDid, data);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submitterDid, req);

            EnsureSuccessResponse(res);
        }
        
        /// <inheritdoc />
        public virtual async Task RegisterRevocationRegistryDefinitionAsync(Wallet wallet, Pool pool, string submitterDid,
            string data)
        {
            var req = await Ledger.BuildRevocRegDefRequestAsync(submitterDid, data);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submitterDid, req);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task SendRevocationRegistryEntryAsync(Wallet wallet, Pool pool, string issuerDid,
            string revocationRegistryDefinitionId, string revocationDefinitionType, string value)
        {
            var req = await Ledger.BuildRevocRegEntryRequestAsync(issuerDid, revocationRegistryDefinitionId,
                revocationDefinitionType, value);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, issuerDid, req);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task RegisterNymAsync(Wallet wallet, Pool pool, string submitterDid, string theirDid,
            string theirVerkey, string role)
        {
            if (DidUtils.IsFullVerkey(theirVerkey))
                theirVerkey = await Did.AbbreviateVerkeyAsync(theirDid, theirVerkey);

            var req = await Ledger.BuildNymRequestAsync(submitterDid, theirDid, theirVerkey, null, role);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submitterDid, req);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task<string> LookupAttributeAsync(Pool pool, string targetDid, string attributeName)
        {
            var req = await Ledger.BuildGetAttribRequestAsync(null, targetDid, attributeName, null, null);
            var res = await Ledger.SubmitRequestAsync(pool, req);
            
            return null;
        }

        /// <inheritdoc />
        public virtual async Task<string> LookupTransactionAsync(Pool pool, string ledgerType, int sequenceId)
        {
            var req = await Ledger.BuildGetTxnRequestAsync(null, ledgerType, sequenceId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return res;
        }

        /// <inheritdoc />
        public virtual async Task RegisterAttributeAsync(Pool pool, Wallet wallet, string submittedDid, string targetDid,
            string attributeName, object value)
        {
            var data = $"{{\"{attributeName}\": {value.ToJson()}}}";

            var req = await Ledger.BuildAttribRequestAsync(submittedDid, targetDid, null, data, null);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submittedDid, req);

            EnsureSuccessResponse(res);
        }

        void EnsureSuccessResponse(string res)
        {
            var response = JObject.Parse(res);

            if (!response["op"].ToObject<string>().Equals("reply", StringComparison.OrdinalIgnoreCase))
                throw new AgentFrameworkException(ErrorCode.LedgerOperationRejected, "Ledger operation rejected");
        }
    }
}