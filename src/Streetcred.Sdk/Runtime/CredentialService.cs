using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    public class CredentialService : ICredentialService
    {
        private readonly IRouterService _routerService;
        private readonly ILedgerService _ledgerService;
        private readonly IConnectionService _connectionService;
        private readonly IWalletRecordService _recordService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly ISchemaService _schemaService;
        private readonly ITailsService _tailsService;
        private readonly IProvisioningService _provisioningService;
        private readonly ILogger<CredentialService> _logger;

        public CredentialService(
            IRouterService routerService,
            ILedgerService ledgerService,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            IMessageSerializer messageSerializer,
            ISchemaService schemaService,
            ITailsService tailsService,
            IProvisioningService provisioningService,
            ILogger<CredentialService> logger)
        {
            _routerService = routerService;
            _ledgerService = ledgerService;
            _connectionService = connectionService;
            _recordService = recordService;
            _messageSerializer = messageSerializer;
            _schemaService = schemaService;
            _tailsService = tailsService;
            _provisioningService = provisioningService;
            _logger = logger;
        }


        /// <inheritdoc />
        public Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId) =>
            _recordService.GetAsync<CredentialRecord>(wallet, credentialId);

        /// <inheritdoc />
        public Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100) =>
            _recordService.SearchAsync<CredentialRecord>(wallet, query, null, count);

        /// <inheritdoc />
        public async Task<string> StoreOfferAsync(Wallet wallet, CredentialOffer credentialOffer)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(credentialOffer.Type);

            var connectionSearch =
                await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credentialOffer.Type}");
            var connection = connectionSearch.First();

            var (offerDetails, _) = await _messageSerializer.UnpackSealedAsync<CredentialOfferDetails>(
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

            await _recordService.AddAsync(wallet, credentialRecord);

            return credentialRecord.GetId();
        }

        /// <inheritdoc />
        public async Task AcceptOfferAsync(Wallet wallet, Pool pool, string credentialId,
            Dictionary<string, string> values)
        {
            var credential = await _recordService.GetAsync<CredentialRecord>(wallet, credentialId);
            var connection = await _connectionService.GetAsync(wallet, credential.ConnectionId);
            var definition =
                await _ledgerService.LookupDefinitionAsync(pool, connection.MyDid, credential.CredentialDefinitionId);
            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

            var request = await AnonCreds.ProverCreateCredentialReqAsync(wallet, connection.MyDid, credential.OfferJson,
                definition.ObjectJson, provisioning.MasterSecretId);

            // Update local credential record with new info and advance the state
            credential.CredentialRequestMetadataJson = request.CredentialRequestMetadataJson;
            await credential.TriggerAsync(CredentialTrigger.Request);
            await _recordService.UpdateAsync(wallet, credential);

            var details = new CredentialRequestDetails
            {
                OfferJson = credential.OfferJson,
                CredentialRequestJson = request.CredentialRequestJson,
                CredentialValuesJson = CredentialUtils.FormatCredentialValues(values)
            };

            var requestMessage =
                await _messageSerializer.PackSealedAsync<CredentialRequest>(details, wallet, connection.MyVk,
                    connection.TheirVk);
            requestMessage.Type =
                MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.CredentialRequest);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = requestMessage.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task StoreCredentialAsync(Pool pool, Wallet wallet, Credential credential)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(credential.Type);

            var connectionSearch =
                await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credential.Type}");
            var connection = connectionSearch.First();

            var (details, _) = await _messageSerializer.UnpackSealedAsync<CredentialDetails>(credential.Content,
                wallet, connection.MyVk);

            var offer = JObject.Parse(details.CredentialJson);
            var definitionId = offer["cred_def_id"].ToObject<string>();
            var schemaId = offer["schema_id"].ToObject<string>();
            var revRegId = offer["rev_reg_id"]?.ToObject<string>();

            var credentialSearch =
                await _recordService.SearchAsync<CredentialRecord>(wallet, new SearchRecordQuery
                {
                    {"schemaId", schemaId},
                    {"definitionId", definitionId},
                    {"connectionId", connection.GetId()}
                }, null, 1);

            var credentialRecord = credentialSearch.Single();
            // TODO: Should throw or resolve conflict gracefully if multiple credential records are found

            var credentialDefinition = await _ledgerService.LookupDefinitionAsync(pool, connection.MyDid, definitionId);

            string revocationRegistryDefinitionJson = null;
            if (!string.IsNullOrEmpty(revRegId))
            {
                // If credential supports revocation, lookup registry definition
                var revocationRegistry =
                    await _ledgerService.LookupRevocationRegistryDefinitionAsync(pool, connection.MyDid, revRegId);
                revocationRegistryDefinitionJson = revocationRegistry.ObjectJson;
            }

            var credentialId = await AnonCreds.ProverStoreCredentialAsync(wallet, null,
                credentialRecord.CredentialRequestMetadataJson,
                details.CredentialJson, credentialDefinition.ObjectJson, revocationRegistryDefinitionJson);

            credentialRecord.CredentialId = credentialId;
            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await _recordService.UpdateAsync(wallet, credentialRecord);
        }


        /// <inheritdoc />
        public async Task<CredentialOffer> CreateOfferAsync(string credentialDefinitionId, string connectionId,
            Wallet wallet, string issuerDid)
        {
            _logger.LogInformation(LoggingEvents.CreateCredentialOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                credentialDefinitionId, connectionId, issuerDid);

            var connection = await _connectionService.GetAsync(wallet, connectionId);
            var offerJson = await AnonCreds.IssuerCreateCredentialOfferAsync(wallet, credentialDefinitionId);
            var nonce = JObject.Parse(offerJson)["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                CredentialDefinitionId = credentialDefinitionId,
                OfferJson = offerJson,
                State = CredentialState.Offered,
                ConnectionId = connection.GetId(),
            };
            credentialRecord.Tags.Add("nonce", nonce);
            credentialRecord.Tags.Add("connectionId", connection.GetId());

            await _recordService.AddAsync(wallet, credentialRecord);

            var credentialOffer = await _messageSerializer.PackSealedAsync<CredentialOffer>(
                new CredentialOfferDetails { OfferJson = offerJson },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            credentialOffer.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.CredentialOffer);

            return credentialOffer;
        }

        /// <inheritdoc />
        public async Task SendOfferAsync(string credentialDefinitionId, string connectionId, Wallet wallet,
            string issuerDid)
        {
            _logger.LogInformation(LoggingEvents.SendCredentialOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                credentialDefinitionId, connectionId, issuerDid);

            var connection = await _connectionService.GetAsync(wallet, connectionId);
            var offer = await CreateOfferAsync(credentialDefinitionId, connectionId, wallet, issuerDid);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = offer.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task<string> StoreCredentialRequestAsync(Wallet wallet, CredentialRequest credentialRequest)
        {
            _logger.LogInformation(LoggingEvents.StoreCredentialRequest, "Type {0},", credentialRequest.Type);

            var (didOrKey, _) = MessageUtils.ParseMessageType(credentialRequest.Type);

            var connectionSearch =
                await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {credentialRequest.Type}");
            var connection = connectionSearch.First();

            var (details, _) = await _messageSerializer.UnpackSealedAsync<CredentialRequestDetails>(
                credentialRequest.Content, wallet, connection.MyVk);

            var request = JObject.Parse(details.OfferJson);
            var nonce = request["nonce"].ToObject<string>();

            var query = new SearchRecordQuery { { "nonce", nonce } };
            var credentialSearch = await _recordService.SearchAsync<CredentialRecord>(wallet, query, null, 1);

            var credential = credentialSearch.Single();
            // Offer should already be present
            // credential.OfferJson = details.OfferJson; 
            credential.ValuesJson = details.CredentialValuesJson;
            credential.RequestJson = details.CredentialRequestJson;

            await credential.TriggerAsync(CredentialTrigger.Request);

            await _recordService.UpdateAsync(wallet, credential);
            return credential.GetId();
        }

        /// <inheritdoc />
        public async Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId)
        {
            var credentialRecord = await _recordService.GetAsync<CredentialRecord>(wallet, credentialId);
            var definitionRecord =
                await _schemaService.GetCredentialDefinitionAsync(wallet, credentialRecord.CredentialDefinitionId);

            var connection = await _connectionService.GetAsync(wallet, credentialRecord.ConnectionId);

            if (credentialRecord.State != CredentialState.Requested)
                throw new Exception(
                    $"Credential sate was invalid. Expected '{CredentialState.Requested}', found '{credentialRecord.State}'");

            string revocationRegistryId = null;
            BlobStorageReader tailsReader = null;
            if (definitionRecord.SupportsRevocation)
            {
                var revocationRecordSearch = await _recordService.SearchAsync<RevocationRegistryRecord>(
                wallet, new SearchRecordQuery { { "credentialDefinitionId", definitionRecord.DefinitionId } }, null, 1);
                var revocationRecord = revocationRecordSearch.First();

                revocationRegistryId = revocationRecord.RevocationRegistryId;
                tailsReader = await _tailsService.OpenTailsAsync(revocationRegistryId);
            }

            var issuedCredential = await AnonCreds.IssuerCreateCredentialAsync(wallet, credentialRecord.OfferJson,
                credentialRecord.RequestJson, credentialRecord.ValuesJson, revocationRegistryId, tailsReader);

            if (definitionRecord.SupportsRevocation)
            {
                await _ledgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
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
            await _recordService.UpdateAsync(wallet, credentialRecord);

            var credential = await _messageSerializer.PackSealedAsync<Credential>(credentialDetails, wallet,
                connection.MyVk,
                connection.TheirVk);
            credential.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Credential);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = credential.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task RevokeCredentialAsync(Pool pool, Wallet wallet, string credentialId, string issuerDid)
        {
            var credential = await GetAsync(wallet, credentialId);
            var definition =
                await _schemaService.GetCredentialDefinitionAsync(wallet, credential.CredentialDefinitionId);

            // Check if the state machine is valid for revocation
            await credential.TriggerAsync(CredentialTrigger.Revoke);

            var revocationRecordSearch = await _recordService.SearchAsync<RevocationRegistryRecord>(
                wallet, new SearchRecordQuery { { "credentialDefinitionId", definition.DefinitionId } }, null, 1);
            var revocationRecord = revocationRecordSearch.First();

            // Revoke the credential
            var tailsReader = await _tailsService.OpenTailsAsync(revocationRecord.TailsFile);
            var revocRegistryDeltaJson = await AnonCreds.IssuerRevokeCredentialAsync(wallet, tailsReader,
                revocationRecord.RevocationRegistryId, credential.CredentialRevocationId);

            // Write the delta state on the ledger for the corresponding revocation registry
            await _ledgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid,
                revocationRecord.RevocationRegistryId,
                "CL_ACCUM", revocRegistryDeltaJson);

            // Update local credential record
            await _recordService.UpdateAsync(wallet, credential);
        }
    }
}