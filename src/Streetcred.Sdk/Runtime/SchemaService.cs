﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    public class SchemaService : ISchemaService
    {
        private readonly IWalletRecordService _recordService;
        private readonly ILedgerService _ledgerService;
        private readonly ITailsService _tailsService;
        readonly IProvisioningService _provisioningService;

        public SchemaService(
            IWalletRecordService recordService,
            ILedgerService ledgerService,
            ITailsService tailsService,
            IProvisioningService provisioningService)
        {
            _provisioningService = provisioningService;
            _recordService = recordService;
            _ledgerService = ledgerService;
            _tailsService = tailsService;
        }

        /// <inheritdoc />
        public async Task<string> CreateSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string name,
            string version, string[] attributeNames)
        {
            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuerDid, name, version, attributeNames.ToJson());

            var schemaRecord = new SchemaRecord {SchemaId = schema.SchemaId};

            await _ledgerService.RegisterSchemaAsync(pool, wallet, issuerDid, schema.SchemaJson);
            await _recordService.AddAsync(wallet, schemaRecord);

            return schemaRecord.SchemaId;
        }

        /// <inheritdoc />
        public async Task<string> CreateCredentialDefinitionAsync(Pool pool, Wallet wallet, string schemaId,
            string issuerDid, bool supportsRevocation, int maxCredentialCount)
        {
            var definitionRecord = new DefinitionRecord();
            var schema = await _ledgerService.LookupSchemaAsync(pool, issuerDid, schemaId);

            var credentialDefinition = await AnonCreds.IssuerCreateAndStoreCredentialDefAsync(wallet, issuerDid,
                schema.ObjectJson, "Tag", null, new {support_revocation = supportsRevocation}.ToJson());

            await _ledgerService.RegisterCredentialDefinitionAsync(wallet, pool, issuerDid,
                credentialDefinition.CredDefJson);

            definitionRecord.Revocable = supportsRevocation;
            definitionRecord.DefinitionId = credentialDefinition.CredDefId;

            if (supportsRevocation)
            {
                var storageId = Guid.NewGuid().ToString().ToLowerInvariant();
                var blobWriter = await _tailsService.GetTailsWriterAsync(storageId);

                var revRegDefConfig =
                    new {issuance_type = "ISSUANCE_ON_DEMAND", max_cred_num = maxCredentialCount}.ToJson();
                var revocationRegistry = await AnonCreds.IssuerCreateAndStoreRevocRegAsync(wallet, issuerDid, null,
                    "Tag2", credentialDefinition.CredDefId, revRegDefConfig, blobWriter);

                await _ledgerService.RegisterRevocationRegistryDefinitionAsync(wallet, pool, issuerDid,
                    revocationRegistry.RevRegDefJson);
                await _ledgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
                    revocationRegistry.RevRegId, "CL_ACCUM", revocationRegistry.RevRegEntryJson);

                // This is a workaround to publish an attribute to the ledger with the tails file location
                // This should be supported by libindy by specifying 'uri_pattern' in the blob storage API, 
                // but it isn't yet supported as of 1.6.6
                var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

                var (attributeName, tailsFilename) = TailsService.GetTailsAtribute(revocationRegistry.RevRegId);
                await _ledgerService.RegisterAttributeAsync(pool, wallet, issuerDid, issuerDid, attributeName,
                    $"{provisioning.TailsBaseUri}/{tailsFilename}");

                definitionRecord.RevocationRegistryId = revocationRegistry.RevRegId;
                definitionRecord.TailsStorageId = storageId;
            }

            await _recordService.AddAsync(wallet, definitionRecord);

            return credentialDefinition.CredDefId;
        }

        /// <inheritdoc />
        /// <exception cref="NotImplementedException"></exception>
        public Task<List<SchemaRecord>> ListSchemasAsync(Wallet wallet) =>
            _recordService.SearchAsync<SchemaRecord>(wallet, null, null, 100);

        /// <summary>
        /// Gets the credential definitions asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<List<DefinitionRecord>> ListCredentialDefinitionsAsync(Wallet wallet) =>
            _recordService.SearchAsync<DefinitionRecord>(wallet, null, null, 100);

        /// <summary>
        /// Gets the credential definition asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialDefinitionId">The credential definition identifier.</param>
        /// <returns></returns>
        public Task<DefinitionRecord> GetCredentialDefinitionAsync(Wallet wallet, string credentialDefinitionId) =>
            _recordService.GetAsync<DefinitionRecord>(wallet, credentialDefinitionId);
    }
}