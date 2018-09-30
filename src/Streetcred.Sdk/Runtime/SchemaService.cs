using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class SchemaService : ISchemaService
    {
        private readonly IWalletRecordService _recordService;
        private readonly ILedgerService _ledgerService;
        private readonly ITailsService _tailsService;

        public SchemaService(
            IWalletRecordService recordService,
            ILedgerService ledgerService,
            ITailsService tailsService)
        {
            _recordService = recordService;
            _ledgerService = ledgerService;
            _tailsService = tailsService;
        }

        /// <inheritdoc />
        public async Task<string> CreateSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string name,
            string version, string[] attributeNames)
        {
            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuerDid, name, version, attributeNames.ToJson());

            var schemaRecord = new SchemaRecord { SchemaId = schema.SchemaId };

            await _ledgerService.RegisterSchemaAsync(pool, wallet, issuerDid, schema.SchemaJson);
            await _recordService.AddAsync(wallet, schemaRecord);

            return schemaRecord.SchemaId;
        }

        /// <inheritdoc />
        public async Task<string> CreateCredentialDefinitionAsync(Pool pool, Wallet wallet, string schemaId,
            string issuerDid, bool supportsRevocation, int maxCredentialCount, Uri tailsBaseUri)
        {
            var definitionRecord = new DefinitionRecord();
            var schema = await _ledgerService.LookupSchemaAsync(pool, issuerDid, schemaId);

            var credentialDefinition = await AnonCreds.IssuerCreateAndStoreCredentialDefAsync(wallet, issuerDid,
                schema.ObjectJson, "Tag", null, new { support_revocation = supportsRevocation }.ToJson());

            await _ledgerService.RegisterCredentialDefinitionAsync(wallet, pool, issuerDid,
                credentialDefinition.CredDefJson);

            definitionRecord.SupportsRevocation = supportsRevocation;
            definitionRecord.DefinitionId = credentialDefinition.CredDefId;

            if (supportsRevocation)
            {
                var tailsHandle = await _tailsService.CreateTailsAsync();

                var revRegDefConfig =
                    new { issuance_type = "ISSUANCE_ON_DEMAND", max_cred_num = maxCredentialCount }.ToJson();
                var revocationRegistry = await AnonCreds.IssuerCreateAndStoreRevocRegAsync(wallet, issuerDid, null,
                    "Tag2", credentialDefinition.CredDefId, revRegDefConfig, tailsHandle);

                // Update tails location URI
                var revocationDefinition = JObject.Parse(revocationRegistry.RevRegDefJson);
                var tailsfile = Path.GetFileName(revocationDefinition["value"]["tailsLocation"].ToObject<string>());
                revocationDefinition["value"]["tailsLocation"] = new Uri(tailsBaseUri, tailsfile).ToString();

                await _ledgerService.RegisterRevocationRegistryDefinitionAsync(wallet, pool, issuerDid,
                    revocationDefinition.ToString());

                await _ledgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
                    revocationRegistry.RevRegId, "CL_ACCUM", revocationRegistry.RevRegEntryJson);

                var revocationRecord = new RevocationRegistryRecord
                {
                    RevocationRegistryId = revocationRegistry.RevRegId,
                    TailsFile = tailsfile
                };
                revocationRecord.Tags["credentialDefinitionId"] = credentialDefinition.CredDefId;
                await _recordService.AddAsync(wallet, revocationRecord);
            }

            await _recordService.AddAsync(wallet, definitionRecord);

            return credentialDefinition.CredDefId;
        }

        /// <inheritdoc />
        public Task<List<SchemaRecord>> ListSchemasAsync(Wallet wallet) =>
            _recordService.SearchAsync<SchemaRecord>(wallet, null, null, 100);

        /// <inheritdoc />
        public Task<List<DefinitionRecord>> ListCredentialDefinitionsAsync(Wallet wallet) =>
            _recordService.SearchAsync<DefinitionRecord>(wallet, null, null, 100);

        /// <inheritdoc />
        public Task<DefinitionRecord> GetCredentialDefinitionAsync(Wallet wallet, string credentialDefinitionId) =>
            _recordService.GetAsync<DefinitionRecord>(wallet, credentialDefinitionId);
    }
}