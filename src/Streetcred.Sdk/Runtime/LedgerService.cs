
using System;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class LedgerService : ILedgerService
    {
        /// <inheritdoc />
        public async Task<ParseResponseResult> LookupDefinitionAsync(Pool pool, string submitterDid,
            string definitionId)
        {
            var req = await Ledger.BuildGetCredDefRequestAsync(submitterDid, definitionId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return await Ledger.ParseGetCredDefResponseAsync(res);
        }

        /// <inheritdoc />
        public async Task<ParseResponseResult> LookupRevocationRegistryDefinitionAsync(Pool pool, string submitterDid,
            string registryId)
        {
            var req = await Ledger.BuildGetRevocRegDefRequestAsync(submitterDid, registryId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return await Ledger.ParseGetRevocRegDefResponseAsync(res);
        }

        /// <inheritdoc />
        public async Task<ParseResponseResult> LookupSchemaAsync(Pool pool, string submitterDid, string schemaId)
        {
            var req = await Ledger.BuildGetSchemaRequestAsync(submitterDid, schemaId);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return await Ledger.ParseGetSchemaResponseAsync(res);
        }

        /// <inheritdoc />
        public async Task<ParseRegistryResponseResult> LookupRevocationRegistryDeltaAsync(Pool pool, string revocationRegistryId,
             long from, long to)
        {
            var req = await Ledger.BuildGetRevocRegDeltaRequestAsync(null, revocationRegistryId, from, to);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            EnsureSuccessResponse(res);

            return await Ledger.ParseGetRevocRegDeltaResponseAsync(res);
        }

        /// <summary>
        /// Registers the schema asynchronous.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="schemaJson">The schema json.</param>
        /// <returns></returns>
        public async Task RegisterSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string schemaJson)
        {
            var req = await Ledger.BuildSchemaRequestAsync(issuerDid, schemaJson);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, issuerDid, req);

            EnsureSuccessResponse(res);
        }
        
        /// <inheritdoc />
        public async Task RegisterCredentialDefinitionAsync(Wallet wallet, Pool pool, string submitterDid, string data)
        {
            var req = await Ledger.BuildCredDefRequestAsync(submitterDid, data);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submitterDid, req);

            EnsureSuccessResponse(res);
        }
        
        /// <inheritdoc />
        public async Task RegisterRevocationRegistryDefinitionAsync(Wallet wallet, Pool pool, string submitterDid,
            string data)
        {
            var req = await Ledger.BuildRevocRegDefRequestAsync(submitterDid, data);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submitterDid, req);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public async Task SendRevocationRegistryEntryAsync(Wallet wallet, Pool pool, string issuerDid,
            string revocationRegistryDefinitionId, string revocationDefinitionType, string value)
        {
            var req = await Ledger.BuildRevocRegEntryRequestAsync(issuerDid, revocationRegistryDefinitionId,
                revocationDefinitionType, value);
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, issuerDid, req);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public async Task RegisterTrustAnchorAsync(Wallet wallet, Pool pool, string submitterDid, string theirDid,
            string theirVerkey)
        {
            if (DidUtils.IsFullVerkey(theirVerkey))
                theirVerkey = await Did.AbbreviateVerkeyAsync(theirDid, theirVerkey);

            var req = await Ledger.BuildNymRequestAsync(submitterDid, theirDid, theirVerkey, null, "TRUST_ANCHOR");
            var res = await Ledger.SignAndSubmitRequestAsync(pool, wallet, submitterDid, req);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public async Task<string> LookupAttributeAsync(Pool pool, string targetDid, string attributeName)
        {
            var req = await Ledger.BuildGetAttribRequestAsync(null, targetDid, attributeName, null, null);
            var res = await Ledger.SubmitRequestAsync(pool, req);

            return null;
        }

        /// <inheritdoc />
        public async Task RegisterAttributeAsync(Pool pool, Wallet wallet, string submittedDid, string targetDid,
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
                throw new Exception("Ledger operation rejected");
        }
    }
}