using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Proofs;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Model.Wallets;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    public class DefaultProofService : IProofService
    {
        protected readonly IRouterService RouterService;
        protected readonly IConnectionService ConnectionService;
        protected readonly IMessageSerializer MessageSerializer;
        protected readonly IWalletRecordService RecordService;
        protected readonly IProvisioningService ProvisioningService;
        protected readonly ILedgerService LedgerService;
        protected readonly ILogger<DefaultProofService> Logger;
        protected readonly ITailsService TailsService;

        public DefaultProofService(
            IConnectionService connectionService,
            IRouterService routerService,
            IMessageSerializer messageSerializer,
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            ILedgerService ledgerService,
            ITailsService tailsService,
            ILogger<DefaultProofService> logger)
        {
            TailsService = tailsService;
            ConnectionService = connectionService;
            RouterService = routerService;
            MessageSerializer = messageSerializer;
            RecordService = recordService;
            ProvisioningService = provisioningService;
            LedgerService = ledgerService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task SendProofRequestAsync(string connectionId, Wallet wallet, ProofRequestObject proofRequest)
        {
            Logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var request = await CreateProofRequestAsync(connectionId, wallet, proofRequest);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = request.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public virtual async Task SendProofRequestAsync(string connectionId, Wallet wallet, string proofRequestJson)
        {
            Logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var request = await CreateProofRequestAsync(connectionId, wallet, proofRequestJson);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = request.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public virtual async Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet,
            ProofRequestObject proofRequestObject)
        {
            if (string.IsNullOrWhiteSpace(proofRequestObject.Nonce))
                throw new ArgumentNullException(nameof(proofRequestObject.Nonce), "Nonce must be set.");

            return await CreateProofRequestAsync(connectionId, wallet, proofRequestObject.ToJson());
        }

        /// <inheritdoc />
        public virtual async Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet,
            string proofRequestJson)
        {
            Logger.LogInformation(LoggingEvents.CreateProofRequest, "ConnectionId {0}", connectionId);

            var connection = await ConnectionService.GetAsync(wallet, connectionId);
            var proofJobj = JObject.Parse(proofRequestJson);

            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connection.ConnectionId,
                RequestJson = proofRequestJson
            };
            proofRecord.Tags["nonce"] = proofJobj["nonce"].ToObject<string>();
            proofRecord.Tags["connectionId"] = connection.GetId();

            await RecordService.AddAsync(wallet, proofRecord);

            var proofRequest = await MessageSerializer.PackSealedAsync<ProofRequest>(
                new ProofRequestDetails {ProofRequestJson = proofRequestJson},
                wallet,
                connection.MyVk,
                connection.TheirVk);
            proofRequest.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.ProofRequest);

            return proofRequest;
        }

        /// <inheritdoc />
        public virtual async Task<string> StoreProofAsync(Wallet wallet, Proof proof)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(proof.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery {{"myDid", didOrKey}});
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {proof.Type}");
            var connection = connectionSearch.First();

            var (requestDetails, _) = await MessageSerializer.UnpackSealedAsync<ProofDetails>(
                proof.Content, wallet, connection.MyVk);
            var proofJson = requestDetails.ProofJson;

            var proofRecordSearch =
                await RecordService.SearchAsync<ProofRecord>(wallet,
                    new SearchRecordQuery {{"nonce", requestDetails.RequestNonce}}, null, 1);
            if (!proofRecordSearch.Any())
                throw new Exception($"Can't find proof record");
            var proofRecord = proofRecordSearch.Single();

            proofRecord.ProofJson = proofJson;
            await proofRecord.TriggerAsync(ProofTrigger.Accept);
            await RecordService.UpdateAsync(wallet, proofRecord);

            return proofRecord.GetId();
        }

        /// <inheritdoc />
        public virtual async Task<string> StoreProofRequestAsync(Wallet wallet, ProofRequest proofRequest)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(proofRequest.Type);

            var connectionSearch =
                await ConnectionService.ListAsync(wallet, new SearchRecordQuery {{"myDid", didOrKey}});
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {proofRequest.Type}");
            var connection = connectionSearch.First();

            var (requestDetails, _) =
                await MessageSerializer.UnpackSealedAsync<ProofRequestDetails>(proofRequest.Content, wallet,
                    connection.MyVk);
            var requestJson = requestDetails.ProofRequestJson;

            var offer = JObject.Parse(requestJson);
            var nonce = offer["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestJson = requestJson,
                ConnectionId = connection.GetId(),
                State = ProofState.Requested
            };
            proofRecord.Tags["connectionId"] = connection.GetId();
            proofRecord.Tags["nonce"] = nonce;

            await RecordService.AddAsync(wallet, proofRecord);

            return proofRecord.GetId();
        }

        /// <inheritdoc />
        public virtual async Task<Proof> CreateProofAsync(Wallet wallet, Pool pool, string proofRequestId,
            RequestedCredentialsDto requestedCredentials)
        {
            var record = await RecordService.GetAsync<ProofRecord>(wallet, proofRequestId);
            var connection = await ConnectionService.GetAsync(wallet, record.ConnectionId);

            var provisioningRecord = await ProvisioningService.GetProvisioningAsync(wallet);

            var credentialObjects = new List<CredentialObject>();
            foreach (var credId in requestedCredentials.GetCredentialIdentifiers())
            {
                credentialObjects.Add(
                    JsonConvert.DeserializeObject<CredentialObject>(
                        await AnonCreds.ProverGetCredentialAsync(wallet, credId)));
            }

            var schemas = await BuildSchemasAsync(pool,
                credentialObjects
                    .Select(x => x.SchemaId)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(pool,
                credentialObjects
                    .Select(x => x.CredentialDefinitionId)
                    .Distinct());

            var revocationStates = await BuildRevocationStatesAsync(pool,
                credentialObjects,
                requestedCredentials);

            var proofJson = await AnonCreds.ProverCreateProofAsync(wallet, record.RequestJson,
                requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
                revocationStates);

            record.ProofJson = proofJson;
            await record.TriggerAsync(ProofTrigger.Accept);
            await RecordService.UpdateAsync(wallet, record);

            var proof = await MessageSerializer.PackSealedAsync<Proof>(
                new ProofDetails
                {
                    ProofJson = proofJson,
                    RequestNonce = JsonConvert.DeserializeObject<ProofRequestObject>(record.RequestJson).Nonce
                }, wallet, connection.MyVk, connection.TheirVk);
            proof.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.DisclosedProof);

            return proof;
        }

        public virtual async Task AcceptProofRequestAsync(Wallet wallet, Pool pool, string proofRequestId,
            RequestedCredentialsDto requestedCredentials)
        {
            var request = await RecordService.GetAsync<ProofRecord>(wallet, proofRequestId);
            var connection = await ConnectionService.GetAsync(wallet, request.ConnectionId);

            var proof = await CreateProofAsync(wallet, pool, proofRequestId, requestedCredentials);

            await RouterService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = proof.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        public virtual Task RejectProofRequestAsync(Wallet wallet, string proofRequestId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual async Task<bool> VerifyProofAsync(Wallet wallet, Pool pool, string proofRecId)
        {
            var proofRecord = await GetAsync(wallet, proofRecId);
            var proofObject = JsonConvert.DeserializeObject<ProofObject>(proofRecord.ProofJson);

            var schemas = await BuildSchemasAsync(pool,
                proofObject.Identifiers
                    .Select(x => x.SchemaId)
                    .Where(x => x != null)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(pool,
                proofObject.Identifiers
                    .Select(x => x.CredentialDefintionId)
                    .Where(x => x != null)
                    .Distinct());

            var revocationDefinitions = await BuildRevocationRegistryDefinitionsAsync(pool,
                proofObject.Identifiers
                    .Select(x => x.RevocationRegistryId)
                    .Where(x => x != null)
                    .Distinct());

            var revocationRegistries = await BuildRevocationRegistryDetlasAsync(pool,
                proofObject.Identifiers
                    .Where(x => x.RevocationRegistryId != null));

            return await AnonCreds.VerifierVerifyProofAsync(proofRecord.RequestJson, proofRecord.ProofJson, schemas,
                definitions, revocationDefinitions, revocationRegistries);
        }

        /// <inheritdoc />
        public virtual Task<List<ProofRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100) =>
            RecordService.SearchAsync<ProofRecord>(wallet, query, null, count);

        /// <inheritdoc />
        public virtual Task<ProofRecord> GetAsync(Wallet wallet, string proofRecId) =>
            RecordService.GetAsync<ProofRecord>(wallet, proofRecId);

        public virtual async Task<List<CredentialInfo>> ListCredentialsForProofRequestAsync(Wallet wallet,
            ProofRequestObject proofRequestObject, string attributeReferent)
        {
            var search =
                await AnonCreds.ProverSearchCredentialsForProofRequestAsync(wallet, proofRequestObject.ToJson());
            var searchResult =
                await AnonCreds.ProverFetchCredentialsForProofRequestAsync(search, attributeReferent, 100);

            await AnonCreds.ProverCloseCredentialsSearchForProofRequestAsync(search);
            return JsonConvert.DeserializeObject<List<CredentialInfo>>(searchResult);
        }

        #region Private Methods

        private async Task<string> BuildSchemasAsync(Pool pool, IEnumerable<string> schemaIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in schemaIds)
            {
                var ledgerSchema = await LedgerService.LookupSchemaAsync(pool, null, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildCredentialDefinitionsAsync(Pool pool, IEnumerable<string> credentialDefIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in credentialDefIds)
            {
                var ledgerDefinition = await LedgerService.LookupDefinitionAsync(pool, null, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerDefinition.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationStatesAsync(Pool pool,
            IEnumerable<CredentialObject> credentialObjects,
            RequestedCredentialsDto requestedCredentials)
        {
            var allCredentials = new List<RequestedAttributeDto>();
            allCredentials.AddRange(requestedCredentials.RequestedAttributes.Values);
            allCredentials.AddRange(requestedCredentials.RequestedPredicates.Values);

            var result = new Dictionary<string, Dictionary<string, JObject>>();
            foreach (var requestedCredential in allCredentials)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                var credential = credentialObjects.First(x => x.Referent == requestedCredential.CredentialId);
                if (credential.RevocationRegistryId == null)
                    continue;

                var timestamp = requestedCredential.Timestamp ??
                                throw new Exception(
                                    "Timestamp must be provided for credential that supports revocation");

                if (result.ContainsKey(credential.RevocationRegistryId) &&
                    result[credential.RevocationRegistryId].ContainsKey($"{timestamp}"))
                {
                    continue;
                }

                var registryDefinition =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool, null,
                        credential.RevocationRegistryId);

                var delta = await LedgerService.LookupRevocationRegistryDeltaAsync(pool,
                    credential.RevocationRegistryId, -1, timestamp);

                var tailsfile = await TailsService.EnsureTailsExistsAsync(pool, credential.RevocationRegistryId);
                var tailsReader = await TailsService.OpenTailsAsync(tailsfile);

                var state = await AnonCreds.CreateRevocationStateAsync(tailsReader, registryDefinition.ObjectJson,
                    delta.ObjectJson, (long) delta.Timestamp, credential.CredentialRevocationId);

                if (!result.ContainsKey(credential.RevocationRegistryId))
                    result.Add(credential.RevocationRegistryId, new Dictionary<string, JObject>());

                result[credential.RevocationRegistryId].Add($"{timestamp}", JObject.Parse(state));

                // TODO: Revocation state should provide the state between a certain period
                // that can be requested in the proof request in the 'non_revocation' field.
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationRegistryDetlasAsync(Pool pool,
            IEnumerable<ProofIdentifier> proofIdentifiers)
        {
            var result = new Dictionary<string, Dictionary<string, JObject>>();

            foreach (var identifier in proofIdentifiers)
            {
                var delta = await LedgerService.LookupRevocationRegistryDeltaAsync(pool,
                    identifier.RevocationRegistryId,
                    -1,
                    long.Parse(identifier.Timestamp));

                result.Add(identifier.RevocationRegistryId,
                    new Dictionary<string, JObject>
                    {
                        {identifier.Timestamp, JObject.Parse(delta.ObjectJson)}
                    });
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationRegistryDefinitionsAsync(Pool pool,
            IEnumerable<string> revocationRegistryIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var revocationRegistryId in revocationRegistryIds)
            {
                var ledgerSchema =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool, null, revocationRegistryId);
                result.Add(revocationRegistryId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        #endregion

    }
}