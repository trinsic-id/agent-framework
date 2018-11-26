using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages.Credentials;
using Streetcred.Sdk.Models.Credentials;
using Streetcred.Sdk.Models.Records;
using Streetcred.Sdk.Models.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultCredentialService : ICredentialService
    {
        protected readonly IRouterService RouterService;
        protected readonly ILedgerService LedgerService;
        protected readonly IConnectionService ConnectionService;
        protected readonly IWalletRecordService RecordService;
        protected readonly IMessageSerializer MessageSerializer;
        protected readonly ISchemaService SchemaService;
        protected readonly ITailsService TailsService;
        protected readonly IProvisioningService ProvisioningService;
        protected readonly ILogger<DefaultCredentialService> Logger;

        public DefaultCredentialService(
            IRouterService routerService,
            ILedgerService ledgerService,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            IMessageSerializer messageSerializer,
            ISchemaService schemaService,
            ITailsService tailsService,
            IProvisioningService provisioningService,
            ILogger<DefaultCredentialService> logger)
        {
            RouterService = routerService;
            LedgerService = ledgerService;
            ConnectionService = connectionService;
            RecordService = recordService;
            MessageSerializer = messageSerializer;
            SchemaService = schemaService;
            TailsService = tailsService;
            ProvisioningService = provisioningService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId) =>
            RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

        /// <inheritdoc />
        public virtual Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100) =>
            RecordService.SearchAsync<CredentialRecord>(wallet, query, null, count);

        /// <inheritdoc />
        public virtual async Task<string> ProcessOfferAsync(Wallet wallet, CredentialOfferMessage credentialOffer, ConnectionRecord connection)
        {
            var offerJson = credentialOffer.OfferJson;
            var offer = JObject.Parse(offerJson);
            var definitionId = offer["cred_def_id"].ToObject<string>();
            var schemaId = offer["schema_id"].ToObject<string>();
            var nonce = offer["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                OfferJson = offerJson,
                ConnectionId = connection.GetId(),
                CredentialDefinitionId = definitionId,
                State = CredentialState.Offered
            };
            credentialRecord.Tags.Add(TagConstants.ConnectionId, connection.GetId());
            credentialRecord.Tags.Add(TagConstants.Role, TagConstants.Holder);
            credentialRecord.Tags.Add(TagConstants.Nonce, nonce);
            credentialRecord.Tags.Add(TagConstants.SchemaId, schemaId);
            credentialRecord.Tags.Add(TagConstants.DefinitionId, definitionId);

            await RecordService.AddAsync(wallet, credentialRecord);

            return credentialRecord.GetId();
        }

        /// <inheritdoc />
        public virtual async Task AcceptOfferAsync(Wallet wallet, Pool pool, string credentialId,
            Dictionary<string, string> attributeValues = null)
        {
            var credential = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);
            var connection = await ConnectionService.GetAsync(wallet, credential.ConnectionId);
            var definition =
                await LedgerService.LookupDefinitionAsync(pool, connection.MyDid, credential.CredentialDefinitionId);
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);

            var request = await AnonCreds.ProverCreateCredentialReqAsync(wallet, connection.MyDid, credential.OfferJson,
                definition.ObjectJson, provisioning.MasterSecretId);

            // Update local credential record with new info
            credential.CredentialRequestMetadataJson = request.CredentialRequestMetadataJson;
            
            await credential.TriggerAsync(CredentialTrigger.Request);
            await RecordService.UpdateAsync(wallet, credential);

            var msg = new CredentialRequestMessage
            {
                OfferJson = credential.OfferJson,
                CredentialRequestJson = request.CredentialRequestJson,
                CredentialValuesJson = CredentialUtils.FormatCredentialValues(attributeValues)
            };

            //TODO we need roll back, i.e if we fail to send the A2A message the credential record should revert to Offer phase
            await RouterService.SendAsync(wallet, msg, connection); 
        }

        /// <inheritdoc />
        public virtual async Task RejectOfferAsync(Wallet wallet, string credentialId)
        {
            var record = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

            await record.TriggerAsync(CredentialTrigger.Reject);
            await RecordService.UpdateAsync(wallet, record);
        }

        /// <inheritdoc />
        public virtual async Task ProcessCredentialAsync(Pool pool, Wallet wallet, CredentialMessage credential, ConnectionRecord connection)
        {
            var offer = JObject.Parse(credential.CredentialJson);
            var definitionId = offer["cred_def_id"].ToObject<string>();
            var schemaId = offer["schema_id"].ToObject<string>();
            var revRegId = offer["rev_reg_id"]?.ToObject<string>();

            var credentialSearch =
                await RecordService.SearchAsync<CredentialRecord>(wallet, new SearchRecordQuery
                {
                    { TagConstants.SchemaId, schemaId},
                    { TagConstants.DefinitionId, definitionId},
                    { TagConstants.ConnectionId, connection.GetId()}
                }, null, 1);

            var credentialRecord = credentialSearch.Single();
            // TODO: Should throw or resolve conflict gracefully if multiple credential records are found

            var credentialDefinition = await LedgerService.LookupDefinitionAsync(pool, connection.MyDid, definitionId);

            string revocationRegistryDefinitionJson = null;
            if (!string.IsNullOrEmpty(revRegId))
            {
                // If credential supports revocation, lookup registry definition
                var revocationRegistry =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool, connection.MyDid, revRegId);
                revocationRegistryDefinitionJson = revocationRegistry.ObjectJson;
            }

            var credentialId = await AnonCreds.ProverStoreCredentialAsync(wallet, null,
                credentialRecord.CredentialRequestMetadataJson,
                credential.CredentialJson, credentialDefinition.ObjectJson, revocationRegistryDefinitionJson);

            credentialRecord.CredentialId = credentialId;
            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(wallet, credentialRecord);
        }

        /// <inheritdoc />
        public virtual async Task<CredentialOfferMessage> CreateOfferAsync(Wallet wallet, OfferConfiguration config)
        {
            Logger.LogInformation(LoggingEvents.CreateCredentialOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                config.CredentialDefinitionId, config.ConnectionId, config.IssuerDid);

            var connection = await ConnectionService.GetAsync(wallet, config.ConnectionId);
            var offerJson = await AnonCreds.IssuerCreateCredentialOfferAsync(wallet, config.CredentialDefinitionId);
            var nonce = JObject.Parse(offerJson)["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                CredentialDefinitionId = config.CredentialDefinitionId,
                OfferJson = offerJson,
                ValuesJson = CredentialUtils.FormatCredentialValues(config.CredentialAttributeValues),
                State = CredentialState.Offered,
                ConnectionId = connection.GetId(),
            };
            credentialRecord.Tags.Add(TagConstants.Nonce, nonce);
            credentialRecord.Tags.Add(TagConstants.Role, TagConstants.Issuer);
            credentialRecord.Tags.Add(TagConstants.ConnectionId, connection.GetId());

            if (!string.IsNullOrEmpty(config.IssuerDid))
                credentialRecord.Tags.Add(TagConstants.IssuerDid, config.IssuerDid);

            if (config.Tags != null)
                foreach (var tag in config.Tags)
                {
                    if (!credentialRecord.Tags.Keys.Contains(tag.Key))
                        credentialRecord.Tags.Add(tag.Key, tag.Value);
                }

            await RecordService.AddAsync(wallet, credentialRecord);

            return new CredentialOfferMessage { OfferJson = offerJson };
        }

        /// <inheritdoc />
        public virtual async Task SendOfferAsync(Wallet wallet, OfferConfiguration config)
        {
            Logger.LogInformation(LoggingEvents.SendCredentialOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                config.CredentialDefinitionId, config.ConnectionId, config.IssuerDid);

            var connection = await ConnectionService.GetAsync(wallet, config.ConnectionId);
            var offer = await CreateOfferAsync(wallet, config);

            await RouterService.SendAsync(wallet, offer, connection);
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessCredentialRequestAsync(Wallet wallet, CredentialRequestMessage credentialRequest, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.StoreCredentialRequest, "Type {0},", credentialRequest.Type);
           
            var request = JObject.Parse(credentialRequest.OfferJson);
            var nonce = request["nonce"].ToObject<string>();

            var query = new SearchRecordQuery { { TagConstants.Nonce , nonce } };
            var credentialSearch = await RecordService.SearchAsync<CredentialRecord>(wallet, query, null, 1);

            var credential = credentialSearch.Single();

            // Offer should already be present
            // credential.OfferJson = details.OfferJson;

            if (!string.IsNullOrEmpty(credentialRequest.CredentialValuesJson) && JObject.Parse(credentialRequest.CredentialValuesJson).Count != 0)
                    credential.ValuesJson = credentialRequest.CredentialValuesJson;

            credential.RequestJson = credentialRequest.CredentialRequestJson;

            await credential.TriggerAsync(CredentialTrigger.Request);

            await RecordService.UpdateAsync(wallet, credential);
            
            return credential.GetId();
        }

        /// <inheritdoc />
        public virtual async Task RejectCredentialRequestAsync(Wallet wallet, string credentialId)
        {
            var record = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

            await record.TriggerAsync(CredentialTrigger.Reject);
            await RecordService.UpdateAsync(wallet, record);
        }

        /// <inheritdoc />
        public virtual Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId)
        {
            return IssueCredentialAsync(pool, wallet, issuerDid, credentialId, null);
        }

        /// <inheritdoc />
        public virtual async Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId,
           Dictionary<string, string> values)
        {
            var credentialRecord = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

            if (values != null && values.Count > 0)
                credentialRecord.ValuesJson = CredentialUtils.FormatCredentialValues(values);

            var definitionRecord =
                await SchemaService.GetCredentialDefinitionAsync(wallet, credentialRecord.CredentialDefinitionId);

            var connection = await ConnectionService.GetAsync(wallet, credentialRecord.ConnectionId);

            if (credentialRecord.State != CredentialState.Requested)
                throw new Exception(
                    $"Credential sate was invalid. Expected '{CredentialState.Requested}', found '{credentialRecord.State}'");

            string revocationRegistryId = null;
            BlobStorageReader tailsReader = null;
            if (definitionRecord.SupportsRevocation)
            {
                var revocationRecordSearch = await RecordService.SearchAsync<RevocationRegistryRecord>(
                wallet, new SearchRecordQuery { { TagConstants.CredentialDefinitionId , definitionRecord.DefinitionId } }, null, 1);
                var revocationRecord = revocationRecordSearch.First();

                revocationRegistryId = revocationRecord.RevocationRegistryId;
                tailsReader = await TailsService.OpenTailsAsync(revocationRecord.TailsFile);
            }

            var issuedCredential = await AnonCreds.IssuerCreateCredentialAsync(wallet, credentialRecord.OfferJson,
                credentialRecord.RequestJson, credentialRecord.ValuesJson, revocationRegistryId, tailsReader);

            if (definitionRecord.SupportsRevocation)
            {
                await LedgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
                    revocationRegistryId,
                    "CL_ACCUM", issuedCredential.RevocRegDeltaJson);
                credentialRecord.CredentialRevocationId = issuedCredential.RevocId;
            }

            var msg = new CredentialMessage
            {
                CredentialJson = issuedCredential.CredentialJson,
                RevocationRegistryId = revocationRegistryId
            };

            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(wallet, credentialRecord);

            await RouterService.SendAsync(wallet, msg, connection);
        }

        /// <inheritdoc />
        public virtual async Task RevokeCredentialAsync(Pool pool, Wallet wallet, string credentialId, string issuerDid)
        {
            var credential = await GetAsync(wallet, credentialId);
            var definition =
                await SchemaService.GetCredentialDefinitionAsync(wallet, credential.CredentialDefinitionId);

            // Check if the state machine is valid for revocation
            await credential.TriggerAsync(CredentialTrigger.Revoke);

            var revocationRecordSearch = await RecordService.SearchAsync<RevocationRegistryRecord>(
                wallet, new SearchRecordQuery { { TagConstants.CredentialDefinitionId , definition.DefinitionId } }, null, 1);
            var revocationRecord = revocationRecordSearch.First();

            // Revoke the credential
            var tailsReader = await TailsService.OpenTailsAsync(revocationRecord.TailsFile);
            var revocRegistryDeltaJson = await AnonCreds.IssuerRevokeCredentialAsync(wallet, tailsReader,
                revocationRecord.RevocationRegistryId, credential.CredentialRevocationId);

            // Write the delta state on the ledger for the corresponding revocation registry
            await LedgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
                revocationRecord.RevocationRegistryId,
                "CL_ACCUM", revocRegistryDeltaJson);

            // Update local credential record
            await RecordService.UpdateAsync(wallet, credential);
        }
    }
}