using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
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
    public class ProofService : IProofService
    {
        private readonly IRouterService _routerService;
        private readonly IConnectionService _connectionService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly IWalletRecordService _recordService;
        private readonly IProvisioningService _provisioningService;
        private readonly ILedgerService _ledgerService;
        private readonly ILogger<ProofService> _logger;
        private readonly ITailsService _tailsService;

        public ProofService(
            IConnectionService connectionService,
            IRouterService routerService,
            IMessageSerializer messageSerializer,
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            ILedgerService ledgerService,
            ITailsService tailsService,
            ILogger<ProofService> logger)
        {
            _tailsService = tailsService;
            _connectionService = connectionService;
            _routerService = routerService;
            _messageSerializer = messageSerializer;
            _recordService = recordService;
            _provisioningService = provisioningService;
            _ledgerService = ledgerService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendProofRequestAsync(string connectionId, Wallet wallet, ProofRequestObject proofRequest)
        {
            _logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await _connectionService.GetAsync(wallet, connectionId);
            var request = await CreateProofRequestAsync(connectionId, wallet, proofRequest);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = request.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task SendProofRequestAsync(string connectionId, Wallet wallet, string proofRequestJson)
        {
            _logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await _connectionService.GetAsync(wallet, connectionId);
            var request = await CreateProofRequestAsync(connectionId, wallet, proofRequestJson);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = request.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet,
            ProofRequestObject proofRequestObject)
        {
            if (string.IsNullOrWhiteSpace(proofRequestObject.Nonce))
                throw new ArgumentNullException(nameof(proofRequestObject.Nonce), "Nonce must be set.");
            // For some reason, search API throws if nonce contians 'dash' symbol

            return await CreateProofRequestAsync(connectionId, wallet, proofRequestObject.ToJson());
        }

        /// <inheritdoc />
        public async Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet,
            string proofRequestJson)
        {
            _logger.LogInformation(LoggingEvents.CreateProofRequest, "ConnectionId {0}", connectionId);

            var connection = await _connectionService.GetAsync(wallet, connectionId);
            var proofJobj = JObject.Parse(proofRequestJson);

            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connection.ConnectionId,
                RequestJson = proofRequestJson
            };
            proofRecord.Tags["nonce"] = proofJobj["nonce"].ToObject<string>();
            proofRecord.Tags["connectionId"] = connection.GetId();

            await _recordService.AddAsync(wallet, proofRecord);

            var proofRequest = await _messageSerializer.PackSealedAsync<ProofRequest>(
                new ProofRequestDetails { ProofRequestJson = proofRequestJson },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            proofRequest.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.ProofRequest);

            return proofRequest;
        }

        public async Task<string> StoreProofAsync(Wallet wallet, Proof proof)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(proof.Type);

            var connectionSearch =
                await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {proof.Type}");
            var connection = connectionSearch.First();

            var (requestDetails, _) = await _messageSerializer.UnpackSealedAsync<ProofDetails>(
                proof.Content, wallet, connection.MyVk);
            var proofJson = requestDetails.ProofJson;

            var proofRecordSearch =
                await _recordService.SearchAsync<ProofRecord>(wallet,
                    new SearchRecordQuery { { "nonce", requestDetails.RequestNonce } }, null, 1);
            if (!proofRecordSearch.Any())
                throw new Exception($"Can't find proof record");
            var proofRecord = proofRecordSearch.Single();

            proofRecord.ProofJson = proofJson;
            await proofRecord.TriggerAsync(ProofTrigger.Accept);
            await _recordService.UpdateAsync(wallet, proofRecord);

            return proofRecord.GetId();
        }

        /// <inheritdoc />
        public async Task<string> StoreProofRequestAsync(Wallet wallet, ProofRequest proofRequest)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(proofRequest.Type);

            var connectionSearch =
                await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {proofRequest.Type}");
            var connection = connectionSearch.First();

            var (requestDetails, _) = await _messageSerializer.UnpackSealedAsync<ProofRequestDetails>(
                proofRequest.Content,
                wallet, connection.MyVk);
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
            proofRecord.Tags.Add("connectionId", connection.GetId());
            proofRecord.Tags.Add("nonce", nonce);

            await _recordService.AddAsync(wallet, proofRecord);

            return proofRecord.GetId();
        }

        /// <inheritdoc />
        public async Task<Proof> CreateProofAsync(Wallet wallet, Pool pool, string proofRequestId,
            RequestedCredentialsDto requestedCredentials)
        {
            var record = await _recordService.GetAsync<ProofRecord>(wallet, proofRequestId);
            var connection = await _connectionService.GetAsync(wallet, record.ConnectionId);
            var requestObject = JsonConvert.DeserializeObject<ProofRequestObject>(record.RequestJson);

            var provisioningRecord = await _provisioningService.GetProvisioningAsync(wallet);

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
                    .Distinct(), connection.MyDid);

            var definitions = await BuildCredentialDefinitionsAsync(pool,
                credentialObjects
                    .Select(x => x.CredentialDefinitionId)
                    .Distinct(), connection.MyDid);

            var revocationStates = await BuildRevocationStatesAsync(pool,
                credentialObjects
                    .Where(x => x.RevocationRegistryId != null),
                    requestedCredentials);

            var proofJson = await AnonCreds.ProverCreateProofAsync(wallet, record.RequestJson,
                requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
                revocationStates);

            record.ProofJson = proofJson;
            await record.TriggerAsync(ProofTrigger.Accept);
            await _recordService.UpdateAsync(wallet, record);

            var proof = await _messageSerializer.PackSealedAsync<Proof>(
                new ProofDetails
                {
                    ProofJson = proofJson,
                    RequestNonce = JsonConvert.DeserializeObject<ProofRequestObject>(record.RequestJson).Nonce
                },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            proof.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.DisclosedProof);

            return proof;
        }

        public async Task AcceptProofRequestAsync(Wallet wallet, Pool pool, string proofRequestId,
            RequestedCredentialsDto requestedCredentials)
        {
            var request = await _recordService.GetAsync<ProofRecord>(wallet, proofRequestId);
            var connection = await _connectionService.GetAsync(wallet, request.ConnectionId);

            var proof = await CreateProofAsync(wallet, pool, proofRequestId, requestedCredentials);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = proof.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

        public Task RejectProofRequestAsync(Wallet wallet, string proofRequestId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<bool> VerifyProofAsync(Wallet wallet, Pool pool, string proofRecId)
        {
            var proofRecord = await GetAsync(wallet, proofRecId);
            var proofObject = JsonConvert.DeserializeObject<ProofObject>(proofRecord.ProofJson);

            var schemas = await BuildSchemasAsync(pool,
                proofObject.Identifiers
                    .Select(x => x.SchemaId)
                    .Where(x => x != null)
                    .Distinct(), null);

            var definitions = await BuildCredentialDefinitionsAsync(pool,
                proofObject.Identifiers
                    .Select(x => x.CredentialDefintionId)
                    .Where(x => x != null)
                    .Distinct(), null);

            var revocationDefinitions = await BuildRevocationRegistryDefintionsAsync(pool,
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
        public Task<List<ProofRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100)
        {
            return _recordService.SearchAsync<ProofRecord>(wallet, query, null, count);
        }

        /// <inheritdoc />
        public Task<ProofRecord> GetAsync(Wallet wallet, string proofRecId)
        {
            return _recordService.GetAsync<ProofRecord>(wallet, proofRecId);
        }

        public async Task<List<CredentialInfo>> ListCredentialsForProofRequestAsync(Wallet wallet,
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

        private async Task<string> BuildSchemasAsync(Pool pool, IEnumerable<string> schemaIds,
            string submitterDid)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in schemaIds)
            {
                var ledgerSchema =
                    await _ledgerService.LookupSchemaAsync(pool, submitterDid,
                        schemaId); // TODO: null support need to be added in dotnet wrapper, its available in libindy
                result.Add(schemaId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildCredentialDefinitionsAsync(Pool pool,
            IEnumerable<string> credentialDefIds,
            string submitterDid)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in credentialDefIds)
            {
                var ledgerDefinition =
                    await _ledgerService.LookupDefinitionAsync(pool, submitterDid,
                        schemaId); // TODO: null support need to be added in dotnet wrapper, its available in libindy
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
                var credential = credentialObjects.First(x => x.Referent == requestedCredential.CredentialId);
                if (credential.RevocationRegistryId == null)
                    continue;

                var timestamp = requestedCredential.Timestamp ?? throw new Exception("Timestamp must be provided for credential that supports revocation");

                if (result.ContainsKey(credential.RevocationRegistryId)
                    && result[credential.RevocationRegistryId].ContainsKey($"{timestamp}"))
                {
                    continue;
                }

                var registryDefinition =
                    await _ledgerService.LookupRevocationRegistryDefinitionAsync(pool, null,
                        credential.RevocationRegistryId);
                var delta = await _ledgerService.LookupRevocationRegistryDeltaAsync(pool,
                    credential.RevocationRegistryId,
                                                                                    -1, timestamp);

                var jobj = JObject.Parse(registryDefinition.ObjectJson);
                var tailsfile = new Uri(jobj["value"]["tailsLocation"].ToObject<string>()).Segments.Last();

                // TODO: Ensure file exists, if not, pass URI to download or sync latest available version
                //await _tailsService.EnsureTailsExistsAsync(pool, credential.RevocationRegistryId);

                var tailsReader = await _tailsService.OpenTailsAsync(tailsfile);

                var state = await AnonCreds.CreateRevocationStateAsync(tailsReader, registryDefinition.ObjectJson,
                    delta.ObjectJson, (long)delta.Timestamp,
                    credential.CredentialRevocationId);

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
                var delta = await _ledgerService.LookupRevocationRegistryDeltaAsync(pool,
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

        private async Task<string> BuildRevocationRegistryDefintionsAsync(Pool pool,
            IEnumerable<string> revocationRegistryIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var revocationRegistryId in revocationRegistryIds)
            {
                var ledgerSchema =
                    await _ledgerService.LookupRevocationRegistryDefinitionAsync(pool, null,
                        revocationRegistryId); // TODO: null support need to be added in dotnet wrapper, its available in libindy
                result.Add(revocationRegistryId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        #endregion

    }
}