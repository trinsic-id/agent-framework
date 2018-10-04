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
        public virtual async Task<string> ProcessOfferAsync(Wallet wallet, CredentialOffer credentialOffer)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(credentialOffer.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
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
            credentialRecord.Tags.Add("connectionId", connection.GetId());
            credentialRecord.Tags.Add("nonce", nonce);
            credentialRecord.Tags.Add("schemaId", schemaId);
            credentialRecord.Tags.Add("definitionId", definitionId);

            await RecordService.AddAsync(wallet, credentialRecord);

            return credentialRecord.GetId();
        }

        /// <inheritdoc />
        public virtual async Task AcceptOfferAsync(Wallet wallet, Pool pool, string credentialId,
            Dictionary<string, string> attributeValues)
        {
            var credential = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);
            var connection = await ConnectionService.GetAsync(wallet, credential.ConnectionId);
            var definition =
                await LedgerService.LookupDefinitionAsync(pool, connection.MyDid, credential.CredentialDefinitionId);
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);

            var request = await AnonCreds.ProverCreateCredentialReqAsync(wallet, connection.MyDid, credential.OfferJson,
                definition.ObjectJson, provisioning.MasterSecretId);

            // Update local credential record with new info and advance the state
            credential.CredentialRequestMetadataJson = request.CredentialRequestMetadataJson;
            await credential.TriggerAsync(CredentialTrigger.Request);
            await RecordService.UpdateAsync(wallet, credential);

            var details = new CredentialRequestDetails
            {
                OfferJson = credential.OfferJson,
                CredentialRequestJson = request.CredentialRequestJson,
                CredentialValuesJson = CredentialUtils.FormatCredentialValues(attributeValues)
            };

            var requestMessage =
                await MessageSerializer.PackSealedAsync<CredentialRequest>(details, wallet, connection.MyVk,
                    connection.TheirVk);
            requestMessage.Type =
                MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.CredentialRequest);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = requestMessage.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public virtual async Task ProcessCredentialAsync(Pool pool, Wallet wallet, Credential credential)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(credential.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
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
                    {"schemaId", schemaId},
                    {"definitionId", definitionId},
                    {"connectionId", connection.GetId()}
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
        public virtual async Task<CredentialOffer> CreateOfferAsync(Wallet wallet, DefaultCreateOfferConfiguration config)
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
                ValuesJson = config.CredentialAttributeValues != null
                    ? JsonConvert.SerializeObject(config.CredentialAttributeValues)
                    : null,
                State = CredentialState.Offered,
                ConnectionId = connection.GetId(),
            };
            credentialRecord.Tags.Add("nonce", nonce);
            credentialRecord.Tags.Add("connectionId", connection.GetId());

            if (!string.IsNullOrEmpty(config.IssuerDid))
                credentialRecord.Tags.Add("issuerDid", config.IssuerDid);

            if (config.Tags != null)
                foreach (var tag in config.Tags)
                {
                    if (!credentialRecord.Tags.Keys.Contains(tag.Key))
                        credentialRecord.Tags.Add(tag.Key, tag.Value);
                }

            await RecordService.AddAsync(wallet, credentialRecord);

            var credentialOffer = await MessageSerializer.PackSealedAsync<CredentialOffer>(
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
        public virtual async Task<string> ProcessCredentialRequestAsync(Wallet wallet, CredentialRequest credentialRequest)
        {
            Logger.LogInformation(LoggingEvents.StoreCredentialRequest, "Type {0},", credentialRequest.Type);

            var (didOrKey, _) = MessageUtils.ParseMessageType(credentialRequest.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credentialRequest.Type}");
            var connection = connectionSearch.First();

            var (details, _) = await MessageSerializer.UnpackSealedAsync<CredentialRequestDetails>(
                credentialRequest.Content, wallet, connection.MyVk);

            var request = JObject.Parse(details.OfferJson);
            var nonce = request["nonce"].ToObject<string>();

            var query = new SearchRecordQuery { { "nonce", nonce } };
            var credentialSearch = await RecordService.SearchAsync<CredentialRecord>(wallet, query, null, 1);

            var credential = credentialSearch.Single();
            
            // Offer should already be present
            // credential.OfferJson = details.OfferJson;

            if (!string.IsNullOrEmpty(details.CredentialValuesJson))
                credential.ValuesJson = details.CredentialValuesJson;

            credential.RequestJson = details.CredentialRequestJson;

            await credential.TriggerAsync(CredentialTrigger.Request);

            await RecordService.UpdateAsync(wallet, credential);

            var issuerDid = credential.Tags.Single(_ => _.Key == "issuerDid").Value;
            
            return credential.GetId();
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

            if (values != null)
            {
                credentialRecord.ValuesJson = CredentialUtils.FormatCredentialValues(values);
            }

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
                wallet, new SearchRecordQuery { { "credentialDefinitionId", definitionRecord.DefinitionId } }, null, 1);
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

            var credential = await MessageSerializer.PackSealedAsync<Credential>(credentialDetails, wallet,
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
                wallet, new SearchRecordQuery { { "credentialDefinitionId", definition.DefinitionId } }, null, 1);
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