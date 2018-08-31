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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Credentials;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    public class AgencyCredentialService : CredentialService, IAgencyCredentialService
    {
        private readonly ILogger<AgencyCredentialService> _logger;

        public AgencyCredentialService(
            IRouterService routerService,
            ILedgerService ledgerService,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            IMessageSerializer messageSerializer,
            ISchemaService schemaService,
            ITailsService tailsService,
            ILogger<AgencyCredentialService> logger)
            : base(routerService,
                ledgerService,
                connectionService,
                recordService,
                messageSerializer,
                schemaService,
                tailsService)
        {
            _logger = logger;
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
                new CredentialOfferDetails {OfferJson = offerJson},
                wallet,
                await Did.KeyForLocalDidAsync(wallet, issuerDid),
                await Did.KeyForLocalDidAsync(wallet, connection.TheirDid));
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

            var query = new SearchRecordQuery {{"nonce", nonce}};
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