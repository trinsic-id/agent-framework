using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
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
    public class HolderCredentialService : CredentialService, IHolderCredentialService
    {
        public HolderCredentialService(
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
            var schemaId = offer["schemaId"].ToObject<string>();
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

            var request = await AnonCreds.ProverCreateCredentialReqAsync(wallet, connection.MyDid, credential.OfferJson,
                definition.ObjectJson, "");

            // Update local credential record with new info and advance the state
            credential.CredentialRequestMetadataJson = request.CredentialRequestMetadataJson;
            await credential.TriggerAsync(CredentialTrigger.Request);
            await RecordService.UpdateAsync(wallet, credential);

            var details = new CredentialRequestDetails
            {
                OfferJson = credential.OfferJson,
                CredentialRequestJson = request.CredentialRequestJson,
                CredentialValuesJson = "TODO" // TODO
            };

            var requestMessage =
                await MessageSerializer.PackSealedAsync<CredentialRequest>(details, wallet, connection.MyDid,
                    connection.TheirDid);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = JsonConvert.SerializeObject(requestMessage),
                To = connection.TheirDid
            }, connection.Endpoint);
        }


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
    }
}
