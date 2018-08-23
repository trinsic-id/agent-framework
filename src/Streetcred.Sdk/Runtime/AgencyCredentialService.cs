using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Credentials;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Runtime
{
    public class AgencyCredentialService : CredentialService, IAgencyCredentialService
    {
        public AgencyCredentialService(
            IRouterService routerService,
            ILedgerService ledgerService,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            IMessageSerializer messageSerializer,
            ISchemaService schemaService,
            ITailsService tailsService)
            : base(routerService,
                ledgerService,
                connectionService,
                recordService,
                messageSerializer,
                schemaService,
                tailsService)
        {
        }

        /// <inheritdoc />
        public async Task<CredentialOffer> CreateOfferAsync(string credentialDefinitionId, string connectionId,
            Wallet wallet, string issuerDid)
        {
            var connection = await ConnectionService.GetAsync(wallet, connectionId);

            // Generate credential offer
            var offerJson = await AnonCreds.IssuerCreateCredentialOfferAsync(wallet, credentialDefinitionId);

            // Extract nonce - used to search for the credential record later
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

            // Send credential offer using A2A
            var offerDetails = new CredentialOfferDetails
            {
                OfferJson = offerJson
            };
            var credentialOffer = await MessageSerializer.PackSealedAsync<CredentialOffer>(
                offerDetails,
                wallet,
                await Did.KeyForLocalDidAsync(wallet, issuerDid),
                await Did.KeyForLocalDidAsync(wallet, connection.TheirDid));
            return credentialOffer;
        }

        /// <inheritdoc />
        public async Task SendOfferAsync(string credentialDefinitionId, string connectionId, Wallet wallet,
            string issuerDid)
        {
            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var offer = await CreateOfferAsync(credentialDefinitionId, connectionId, wallet, issuerDid);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = JsonConvert.SerializeObject(offer),
                To = connection.TheirDid
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task StoreCredentialRequest(Wallet wallet, CredentialRequest credentialRequest,
            string connectionId)
        {
            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var (details, _) = await MessageSerializer.UnpackSealedAsync<CredentialRequestDetails>(credentialRequest.Content, wallet,
                await Did.KeyForLocalDidAsync(wallet, connection.MyDid));

            var request = JObject.Parse(details.CredentialRequestJson);
            var nonce = request["nonce"].ToObject<string>();

            var credentialSearch =
                await RecordService.SearchAsync<CredentialRecord>(wallet, new SearchRecordQuery { { "nonce", nonce } },
                    null);

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
                await LedgerService.SendRevocationRegistryEntryAsync(wallet, pool, issuerDid, issuedCredential.RevocId,
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
                Content = JsonConvert.SerializeObject(credential),
                To = connection.TheirDid
            }, connection.Endpoint);
        }

    }
}
