using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultSchemaService : ISchemaService
    {
        /// <summary>The provisioning service</summary>
        // ReSharper disable InconsistentNaming
        protected readonly IProvisioningService ProvisioningService;
        /// <summary>The record service</summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>The ledger service</summary>
        protected readonly ILedgerService LedgerService;
        /// <summary>The tails service</summary>
        protected readonly ITailsService TailsService;   
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSchemaService"/> class.
        /// </summary>
        /// <param name="provisioningService">Provisioning service.</param>
        /// <param name="recordService">Record service.</param>
        /// <param name="ledgerService">Ledger service.</param>
        /// <param name="tailsService">Tails service.</param>
        public DefaultSchemaService(
            IProvisioningService provisioningService,
            IWalletRecordService recordService,
            ILedgerService ledgerService,
            ITailsService tailsService)
        {
            ProvisioningService = provisioningService;
            RecordService = recordService;
            LedgerService = ledgerService;
            TailsService = tailsService;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string name,
            string version, string[] attributeNames)
        {
            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuerDid, name, version, attributeNames.ToJson());

            var schemaRecord = new SchemaRecord
            {
                Id = schema.SchemaId, 
                Name = name, 
                Version = version, 
                AttributeNames = attributeNames
            };

            await LedgerService.RegisterSchemaAsync(pool, wallet, issuerDid, schema.SchemaJson);
            await RecordService.AddAsync(wallet, schemaRecord);

            return schemaRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateSchemaAsync(Pool pool, Wallet wallet, string name,
            string version, string[] attributeNames)
        {
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            if (provisioning?.IssuerDid == null)
            {
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "This wallet is not provisioned with issuer");
            }

            return await CreateSchemaAsync(pool, wallet, provisioning.IssuerDid, name, version, attributeNames);
        }

        /// <inheritdoc />
        public async Task<string> LookupSchemaFromCredentialDefinitionAsync(Pool pool,
            string credentialDefinitionId)
        {
            var credDef = await LookupCredentialDefinitionAsync(pool, credentialDefinitionId);

            if (string.IsNullOrEmpty(credDef))
                return null;

            try
            {
                var schemaSequenceId = Convert.ToInt32(JObject.Parse(credDef)["schemaId"].ToString());
                return await LookupSchemaAsync(pool, schemaSequenceId);
            }
            catch (Exception) { }

            return null;
        }

        /// TODO this should return a schema object
        /// <inheritdoc />
        public virtual async Task<string> LookupSchemaAsync(Pool pool, int sequenceId)
        {
            var result = await LedgerService.LookupTransactionAsync(pool, null, sequenceId);

            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    var txnData = JObject.Parse(result)["result"]["data"]["txn"]["data"]["data"] as JObject;
                    var txnId = JObject.Parse(result)["result"]["data"]["txnMetadata"]["txnId"].ToString();

                    int seperator = txnId.LastIndexOf(':');

                    string ver = txnId.Substring(seperator + 1, txnId.Length - seperator - 1);

                    txnData.Add("id", txnId);
                    txnData.Add("ver", ver);
                    txnData.Add("seqNo", sequenceId);

                    return txnData.ToString();
                }
                catch (Exception) { }
            }

            return null;
        }

        /// TODO this should return a schema object
        /// <inheritdoc />
        public virtual async Task<string> LookupSchemaAsync(Pool pool, string schemaId)
        {
            var result = await LedgerService.LookupSchemaAsync(pool, schemaId);
            return result?.ObjectJson;
        }

        /// <inheritdoc />
        public virtual Task<List<SchemaRecord>> ListSchemasAsync(Wallet wallet) =>
            RecordService.SearchAsync<SchemaRecord>(wallet, null, null, 100);

        /// <inheritdoc />
        public virtual async Task<string> CreateCredentialDefinitionAsync(Pool pool, Wallet wallet, string schemaId,
            string issuerDid, string tag, bool supportsRevocation, int maxCredentialCount, Uri tailsBaseUri)
        {
            var definitionRecord = new DefinitionRecord();
            var schema = await LedgerService.LookupSchemaAsync(pool, schemaId);

            var credentialDefinition = await AnonCreds.IssuerCreateAndStoreCredentialDefAsync(wallet, issuerDid,
                schema.ObjectJson, tag, null, new { support_revocation = supportsRevocation }.ToJson());

            await LedgerService.RegisterCredentialDefinitionAsync(wallet, pool, issuerDid,
                credentialDefinition.CredDefJson);

            definitionRecord.SupportsRevocation = supportsRevocation;
            definitionRecord.Id = credentialDefinition.CredDefId;
            definitionRecord.SchemaId = schemaId;

            if (supportsRevocation)
            {
                definitionRecord.MaxCredentialCount = maxCredentialCount;
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
                    Id = revocationRegistry.RevRegId,
                    TailsFile = tailsfile,
                    CredentialDefinitionId = credentialDefinition.CredDefId
                };
                await RecordService.AddAsync(wallet, revocationRecord);
            }

            await RecordService.AddAsync(wallet, definitionRecord);

            return credentialDefinition.CredDefId;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateCredentialDefinitionAsync(Pool pool, Wallet wallet, string schemaId,
            string tag, bool supportsRevocation, int maxCredentialCount)
        {
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            if (provisioning?.IssuerDid == null)
            {
                throw new AgentFrameworkException(ErrorCode.RecordNotFound,
                    "This wallet is not provisioned with issuer");
            }

            return await CreateCredentialDefinitionAsync(pool, wallet, schemaId, provisioning.IssuerDid, tag,
                supportsRevocation, maxCredentialCount, new Uri(provisioning.TailsBaseUri));
        }

        /// TODO this should return a definition object
        /// <inheritdoc />
        public virtual async Task<string> LookupCredentialDefinitionAsync(Pool pool, string definitionId)
        {
            var result = await LedgerService.LookupDefinitionAsync(pool, definitionId);
            return result?.ObjectJson;
        }

        /// <inheritdoc />
        public virtual Task<List<DefinitionRecord>> ListCredentialDefinitionsAsync(Wallet wallet) =>
            RecordService.SearchAsync<DefinitionRecord>(wallet, null, null, 100);

        /// <inheritdoc />
        public virtual Task<DefinitionRecord> GetCredentialDefinitionAsync(Wallet wallet, string credentialDefinitionId) =>
            RecordService.GetAsync<DefinitionRecord>(wallet, credentialDefinitionId);
    }
}