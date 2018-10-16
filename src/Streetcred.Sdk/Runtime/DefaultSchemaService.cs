using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Models.Records;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultSchemaService : ISchemaService
    {
        protected readonly IWalletRecordService RecordService;
        protected readonly ILedgerService LedgerService;
        protected readonly ITailsService TailsService;

        public DefaultSchemaService(
            IWalletRecordService recordService,
            ILedgerService ledgerService,
            ITailsService tailsService)
        {
            RecordService = recordService;
            LedgerService = ledgerService;
            TailsService = tailsService;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string name,
            string version, string[] attributeNames)
        {
            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuerDid, name, version, attributeNames.ToJson());

            var schemaRecord = new SchemaRecord { SchemaId = schema.SchemaId };

            await LedgerService.RegisterSchemaAsync(pool, wallet, issuerDid, schema.SchemaJson);
            await RecordService.AddAsync(wallet, schemaRecord);

            return schemaRecord.SchemaId;
        }

        /// TODO this should return a definition object
        /// <inheritdoc />
        public virtual async Task<string> LookupDefinitionAsync(Pool pool, Wallet wallet, string submitterDid, int sequenceId)
        {
            var result = await LedgerService.LookupTransactionAsync(pool, submitterDid, sequenceId);
            return result;
        }

        /// TODO this should return a definition object
        /// <inheritdoc />
        public virtual async Task<string> LookupDefinitionAsync(Pool pool, Wallet wallet, string submitterDid, string definitionId)
        {
            var result = await LedgerService.LookupDefinitionAsync(pool, submitterDid, definitionId);
            return result?.ObjectJson;
        }

        /// TODO this should return a schema object
        /// <inheritdoc />
        public virtual async Task<string> LookupSchemaAsync(Pool pool, Wallet wallet, string submitterDid, int sequenceId)
        {
            var result = await LedgerService.LookupTransactionAsync(pool, submitterDid, sequenceId);
            return result;
        }

        /// TODO this should return a schema object
        /// <inheritdoc />
        public virtual async Task<string> LookupSchemaAsync(Pool pool, Wallet wallet, string submitterDid, string schemaId)
        {
            var result = await LedgerService.LookupSchemaAsync(pool, submitterDid, schemaId);
            return result?.ObjectJson;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateCredentialDefinitionAsync(Pool pool, Wallet wallet, string schemaId,
            string issuerDid, bool supportsRevocation, int maxCredentialCount, Uri tailsBaseUri)
        {
            var definitionRecord = new DefinitionRecord();
            var schema = await LedgerService.LookupSchemaAsync(pool, issuerDid, schemaId);

            var credentialDefinition = await AnonCreds.IssuerCreateAndStoreCredentialDefAsync(wallet, issuerDid,
                schema.ObjectJson, "Tag", null, new { support_revocation = supportsRevocation }.ToJson());

            await LedgerService.RegisterCredentialDefinitionAsync(wallet, pool, issuerDid,
                credentialDefinition.CredDefJson);

            definitionRecord.SupportsRevocation = supportsRevocation;
            definitionRecord.DefinitionId = credentialDefinition.CredDefId;

            definitionRecord.Tags.Add(TagConstants.SchemaId, schemaId);

            if (supportsRevocation)
            {
                var tailsHandle = await TailsService.CreateTailsAsync();

                var revRegDefConfig =
                    new { issuance_type = "ISSUANCE_ON_DEMAND", max_cred_num = maxCredentialCount }.ToJson();
                var revocationRegistry = await AnonCreds.IssuerCreateAndStoreRevocRegAsync(wallet, issuerDid, null,
                    "Tag2", credentialDefinition.CredDefId, revRegDefConfig, tailsHandle);

                // Update tails location URI
                var revocationDefinition = JObject.Parse(revocationRegistry.RevRegDefJson);
                var tailsfile = Path.GetFileName(revocationDefinition["value"]["tailsLocation"].ToObject<string>());
                revocationDefinition["value"]["tailsLocation"] = new Uri(tailsBaseUri, tailsfile).ToString();

                await LedgerService.RegisterRevocationRegistryDefinitionAsync(wallet, pool, issuerDid,
                    revocationDefinition.ToString());

                await LedgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
                    revocationRegistry.RevRegId, "CL_ACCUM", revocationRegistry.RevRegEntryJson);

                var revocationRecord = new RevocationRegistryRecord
                {
                    RevocationRegistryId = revocationRegistry.RevRegId,
                    TailsFile = tailsfile
                };
                revocationRecord.Tags[TagConstants.CredentialDefinitionId] = credentialDefinition.CredDefId;
                await RecordService.AddAsync(wallet, revocationRecord);
            }

            await RecordService.AddAsync(wallet, definitionRecord);

            return credentialDefinition.CredDefId;
        }

        /// <inheritdoc />
        public virtual Task<List<SchemaRecord>> ListSchemasAsync(Wallet wallet) =>
            RecordService.SearchAsync<SchemaRecord>(wallet, null, null, 100);

        /// <inheritdoc />
        public virtual Task<List<DefinitionRecord>> ListCredentialDefinitionsAsync(Wallet wallet) =>
            RecordService.SearchAsync<DefinitionRecord>(wallet, null, null, 100);

        /// <inheritdoc />
        public virtual Task<DefinitionRecord> GetCredentialDefinitionAsync(Wallet wallet, string credentialDefinitionId) =>
            RecordService.GetAsync<DefinitionRecord>(wallet, credentialDefinitionId);
    }
}