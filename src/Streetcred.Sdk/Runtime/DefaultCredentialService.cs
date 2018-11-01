using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages.Credentials;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
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
        public virtual async Task<string> ProcessOfferAsync(Wallet wallet, CredentialOfferMessage credentialOffer)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(credentialOffer.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery { { TagConstants.MyDid, didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credentialOffer.Type}");
            var connection = connectionSearch.First();

            var (offerDetails, _) = await MessageSerializer.UnpackSealedAsync<CredentialOfferDetails>(
                credentialOffer.Content, wallet, connection.MyVk);
            var offerJson = offerDetails.OfferJson;

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

            var details = new CredentialRequestDetails
            {
                OfferJson = credential.OfferJson,
                CredentialRequestJson = request.CredentialRequestJson,
                CredentialValuesJson = CredentialUtils.FormatCredentialValues(attributeValues)
            };

            var requestMessage =
                await MessageSerializer.PackSealedAsync<CredentialRequestMessage>(details, wallet, connection.MyVk,
                    connection.TheirVk);
            requestMessage.Type =
                MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.CredentialRequest);

            await credential.TriggerAsync(CredentialTrigger.Request);
            await RecordService.UpdateAsync(wallet, credential);

            //TODO we need roll back, i.e if we fail to send the A2A message the credential record should revert to Offer phase
            //so the user can resend
            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = requestMessage.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public virtual async Task RejectOfferAsync(Wallet wallet, string credentialId)
        {
            var record = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

            await record.TriggerAsync(CredentialTrigger.Reject);
            await RecordService.UpdateAsync(wallet, record);
        }

        /// <inheritdoc />
        public virtual async Task ProcessCredentialAsync(Pool pool, Wallet wallet, CredentialMessage credential)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(credential.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery { { TagConstants.MyDid, didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credential.Type}");
            var connection = connectionSearch.First();

            var (details, _) = await MessageSerializer.UnpackSealedAsync<CredentialDetails>(credential.Content,
                wallet, connection.MyVk);

            var offer = JObject.Parse(details.CredentialJson);
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
                details.CredentialJson, credentialDefinition.ObjectJson, revocationRegistryDefinitionJson);

            credentialRecord.CredentialId = credentialId;
            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(wallet, credentialRecord);
        }

        /// <inheritdoc />
        public virtual async Task<CredentialOfferMessage> CreateOfferAsync(Wallet wallet, DefaultCreateOfferConfiguration config)
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

            var credentialOffer = await MessageSerializer.PackSealedAsync<CredentialOfferMessage>(
                new CredentialOfferDetails { OfferJson = offerJson },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            credentialOffer.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.CredentialOffer);

            return credentialOffer;
        }

        /// <inheritdoc />
        public virtual async Task SendOfferAsync(Wallet wallet, DefaultCreateOfferConfiguration config)
        {
            Logger.LogInformation(LoggingEvents.SendCredentialOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                config.CredentialDefinitionId, config.ConnectionId, config.IssuerDid);

            var connection = await ConnectionService.GetAsync(wallet, config.ConnectionId);
            var offer = await CreateOfferAsync(wallet, config);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = offer.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessCredentialRequestAsync(Wallet wallet, CredentialRequestMessage credentialRequest)
        {
            Logger.LogInformation(LoggingEvents.StoreCredentialRequest, "Type {0},", credentialRequest.Type);

            var (didOrKey, _) = MessageUtils.ParseMessageType(credentialRequest.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery { { TagConstants.MyDid, didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credentialRequest.Type}");
            var connection = connectionSearch.First();

            var (details, _) = await MessageSerializer.UnpackSealedAsync<CredentialRequestDetails>(
                credentialRequest.Content, wallet, connection.MyVk);

            var request = JObject.Parse(details.OfferJson);
            var nonce = request["nonce"].ToObject<string>();

            var query = new SearchRecordQuery { { TagConstants.Nonce , nonce } };
            var credentialSearch = await RecordService.SearchAsync<CredentialRecord>(wallet, query, null, 1);

            var credential = credentialSearch.Single();

            // Offer should already be present
            // credential.OfferJson = details.OfferJson;

            if (!string.IsNullOrEmpty(details.CredentialValuesJson) && JObject.Parse(details.CredentialValuesJson).Count != 0)
                    credential.ValuesJson = details.CredentialValuesJson;

            credential.RequestJson = details.CredentialRequestJson;

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

            var credentialDetails = new CredentialDetails
            {
                CredentialJson = issuedCredential.CredentialJson,
                RevocationRegistryId = revocationRegistryId
            };

            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(wallet, credentialRecord);

            var credential = await MessageSerializer.PackSealedAsync<CredentialMessage>(credentialDetails, wallet,
                connection.MyVk,
                connection.TheirVk);
            credential.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Credential);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = credential.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
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