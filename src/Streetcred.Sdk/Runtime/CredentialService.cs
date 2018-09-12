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
        protected readonly IRouterService RouterService;
        protected readonly ILedgerService LedgerService;
        protected readonly IConnectionService ConnectionService;
        protected readonly IWalletRecordService RecordService;
        protected readonly IMessageSerializer MessageSerializer;
        protected readonly ISchemaService SchemaService;
        protected readonly ITailsService TailsService;
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
            RouterService = routerService;
            LedgerService = ledgerService;
            ConnectionService = connectionService;
            RecordService = recordService;
            MessageSerializer = messageSerializer;
            SchemaService = schemaService;
            TailsService = tailsService;
            _provisioningService = provisioningService;
            _logger = logger;
        }


        /// <inheritdoc />
        public Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId) =>
            RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

        /// <inheritdoc />
        public Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null) =>
            RecordService.SearchAsync<CredentialRecord>(wallet, query, null);

        /// <inheritdoc />
        public async Task<string> StoreOfferAsync(Wallet wallet, CredentialOffer credentialOffer,
            string connectionId)
        {
            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var (offerDetails, _) = await MessageSerializer.UnpackSealedAsync<CredentialOfferDetails>(
                credentialOffer.Content,
                wallet, await Did.KeyForLocalDidAsync(wallet, connection.MyDid));
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
                State = CredentialState.Offered,
                Tags = new Dictionary<string, string>
                {
                    {"connectionId", connection.GetId()},
                    {"nonce", nonce},
                    {"schemaId", schemaId},
                    {"definitionId", definitionId}
                }
            };
            await RecordService.AddAsync(wallet, credentialRecord);

            return credentialRecord.GetId();
        }

        /// <inheritdoc />
        public async Task AcceptOfferAsync(Wallet wallet, Pool pool, string credentialId, Dictionary<string, string> values)
        {
            var credential = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);
            var connection = await ConnectionService.GetAsync(wallet, credential.ConnectionId);
            var definition = await LedgerService.LookupDefinitionAsync(pool, connection.MyDid, credential.CredentialDefinitionId);
            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

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
                CredentialValuesJson = FormatCredentialValues(values)
            };

            var requestMessage =
                await MessageSerializer.PackSealedAsync<CredentialRequest>(details, wallet, connection.MyVk,
                                                                           connection.TheirVk);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = requestMessage.ToJson(),
                To = connection.TheirDid
            }, connection.Endpoint);
        }

        private string FormatCredentialValues(Dictionary<string, string> values)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in values)
            {
                result.Add(item.Key, EncodeValue(item.Value));
            }
            return result.ToJson();
        }

        private Dictionary<string, string> EncodeValue(string value) => new Dictionary<string, string>
        {
            { "raw", value },
            { "encoded", "1234567890" } // TODO: Add value encoding
        };


        /// <inheritdoc />
        public async Task StoreCredentialAsync(Pool pool, Wallet wallet, Credential credential, string connectionId)
        {
            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var (details, _) = await MessageSerializer.UnpackSealedAsync<CredentialDetails>(credential.Content,
                wallet, await Did.KeyForLocalDidAsync(wallet, connection.MyDid));

            var offer = JObject.Parse(details.CredentialJson);
            var definitionId = offer["cred_def_id"].ToObject<string>();
            var schemaId = offer["schemaId"].ToObject<string>();
            var revRegId = offer["rev_reg_id"]?.ToObject<string>();

            var credentialSearch =
                await RecordService.SearchAsync<CredentialRecord>(wallet, new SearchRecordQuery
                {
                    {"schemaId", schemaId},
                    {"definitionId", definitionId},
                    {"connectionId", connectionId}
                }, null);

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
        public async Task<CredentialOffer> CreateOfferAsync(string credentialDefinitionId, string connectionId,
            Wallet wallet, string issuerDid)
        {
            _logger.LogInformation(LoggingEvents.CreateOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                credentialDefinitionId, connectionId, issuerDid);

            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var offerJson = await AnonCreds.IssuerCreateCredentialOfferAsync(wallet, credentialDefinitionId);
            var nonce = JObject.Parse(offerJson)["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                CredentialDefinitionId = credentialDefinitionId,
                OfferJson = offerJson,
                State = CredentialState.Offered,
                Tags = new Dictionary<string, string>
                {
                    {"nonce", nonce},
                    {"connectionId", connection.GetId()}
                }
            };
            await RecordService.AddAsync(wallet, credentialRecord);

            var credentialOffer = await MessageSerializer.PackSealedAsync<CredentialOffer>(
                new CredentialOfferDetails { OfferJson = offerJson },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            return credentialOffer;
        }

        /// <inheritdoc />
        public async Task SendOfferAsync(string credentialDefinitionId, string connectionId, Wallet wallet,
            string issuerDid)
        {
            _logger.LogInformation(LoggingEvents.SendOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
                credentialDefinitionId, connectionId, issuerDid);

            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var offer = await CreateOfferAsync(credentialDefinitionId, connectionId, wallet, issuerDid);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = offer.ToJson(),
                To = connection.TheirDid
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task StoreCredentialRequestAsync(Wallet wallet, CredentialRequest credentialRequest,
            string connectionId)
        {
            _logger.LogInformation(LoggingEvents.StoreCredentialRequest, "ConnectionId {0},", connectionId);

            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var (details, _) = await MessageSerializer.UnpackSealedAsync<CredentialRequestDetails>(
                credentialRequest.Content, wallet,
                await Did.KeyForLocalDidAsync(wallet, connection.MyDid));

            var request = JObject.Parse(details.CredentialRequestJson);
            var nonce = request["nonce"].ToObject<string>();

            var query = new SearchRecordQuery { { "nonce", nonce } };
            var credentialSearch = await RecordService.SearchAsync<CredentialRecord>(wallet, query, null);

            var credential = credentialSearch.Single();
            // Offer should already be present
            // credential.OfferJson = details.OfferJson; 
            credential.ValuesJson = details.CredentialValuesJson;
            credential.RequestJson = details.CredentialRequestJson;

            await RecordService.UpdateAsync(wallet, credential);
        }

        /// <inheritdoc />
        public async Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId)
        {
            var credentialRecord = await RecordService.GetAsync<CredentialRecord>(wallet, credentialId);
            var definitionRecord =
                await SchemaService.GetCredentialDefinitionAsync(wallet, credentialRecord.CredentialDefinitionId);

            var connection = await ConnectionService.GetAsync(wallet, credentialRecord.ConnectionId);

            if (credentialRecord.State != CredentialState.Requested)
                throw new Exception(
                    $"Credential sate was invalid. Expected '{CredentialState.Requested}', found '{credentialRecord.State}'");

            string revocationRegistryId = null;
            BlobStorageReader tailsReader = null;
            if (definitionRecord.Revocable)
            {
                revocationRegistryId = credentialRecord.RevocId;
                tailsReader = await TailsService.GetBlobStorageReaderAsync(definitionRecord.TailsStorageId);
            }

            var issuedCredential = await AnonCreds.IssuerCreateCredentialAsync(wallet, credentialRecord.OfferJson,
                credentialRecord.RequestJson, credentialRecord.ValuesJson, revocationRegistryId, tailsReader);

            if (definitionRecord.Revocable)
            {
                await LedgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid, definitionRecord.RevocationRegistryId,
                    "CL_ACCUM", issuedCredential.RevocRegDeltaJson);
            }

            var credentialDetails = new CredentialDetails
            {
                CredentialJson = issuedCredential.CredentialJson,
                RevocationRegistryId = issuedCredential.RevocId
            };

            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(wallet, credentialRecord);

            var credential = await MessageSerializer.PackSealedAsync<Credential>(credentialDetails, wallet,
                await Did.KeyForLocalDidAsync(wallet, connection.MyDid),
                await Did.KeyForLocalDidAsync(wallet, connection.TheirDid));

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = credential.ToJson(),
                To = connection.TheirDid
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task RevokeCredentialAsync(Pool pool, Wallet wallet, string credentialId, string issuerDid)
        {
            var credential = await GetAsync(wallet, credentialId);
            var definition = await SchemaService.GetCredentialDefinitionAsync(wallet, credential.CredentialDefinitionId);

            // Check if the state machine is valid for revocation
            await credential.TriggerAsync(CredentialTrigger.Revoke);

            // Revoke the credential
            var tailsReader = await TailsService.GetBlobStorageReaderAsync(definition.TailsStorageId);
            var revocRegistryDeltaJson = await AnonCreds.IssuerRevokeCredentialAsync(wallet, tailsReader,
                                                                                     definition.RevocationRegistryId,
                                                                                     credential.RevocId);

            // Write the delta state on the ledger for the corresponding revocation registry
            await LedgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid, definition.RevocationRegistryId,
                                                                 "CL_ACCUM", revocRegistryDeltaJson);

            // Update local credential record
            await RecordService.UpdateAsync(wallet, credential);
        }
    }
}
