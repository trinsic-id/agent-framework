using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Proofs;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
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
        private readonly ISchemaService _schemaService;
        private readonly ICredentialService _credentialService;
        private readonly ILogger<ProofService> _logger;

        public ProofService(IConnectionService connectionService,
                            IRouterService routerService,
                            IMessageSerializer messageSerializer,
                            IWalletRecordService recordService,
                            IProvisioningService provisioningService,
                            ISchemaService schemaService,
                            ICredentialService credentialService,
                            ILogger<ProofService> logger)
        {
            _connectionService = connectionService;
            _routerService = routerService;
            _messageSerializer = messageSerializer;
            _recordService = recordService;
            _provisioningService = provisioningService;
            _schemaService = schemaService;
            _credentialService = credentialService;
            _logger = logger;
        }
        
        //TODO attributes will change to a more complicated object
        public async Task SendProofRequestAsync(string connectionId, Wallet wallet, IEnumerable<string> attributes)
        {
            _logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await _connectionService.GetAsync(wallet, connectionId);
            var request = await CreateProofRequestAsync(connectionId, wallet, attributes);

            await _routerService.ForwardAsync(new ForwardEnvelopeMessage
            {
                Content = request.ToJson(),
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward)
            }, connection.Endpoint);
        }

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

        //TODO attributes will change to a more complicated object
        public async Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet, IEnumerable<string> attributes)
        {
            var proofJson = ProofUtils.CreateProofRequest(attributes);
            return await CreateProofRequestAsync(connectionId, wallet, proofJson);
        }

        public async Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet, string proofRequestJson)
        {
            _logger.LogInformation(LoggingEvents.CreateProofRequest, "ConnectionId {0}", connectionId);

            var connection = await _connectionService.GetAsync(wallet, connectionId);

            var proofRequest = await _messageSerializer.PackSealedAsync<ProofRequest>(
                new ProofRequestDetails() { ProofRequestJson = proofRequestJson },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            proofRequest.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.ProofRequest);
            

            return proofRequest;
        }

        public async Task<string> StoreProofAsync(Wallet wallet, Proof proof)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(proof.Type);

            var connectionSearch = await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {proof.Type}");
            var connection = connectionSearch.First();

            var (requestDetails, _) = await _messageSerializer.UnpackSealedAsync<ProofDetails>(
                proof.Content,
                wallet, await Did.KeyForLocalDidAsync(wallet, connection.MyDid));
            var proofJson = requestDetails.ProofJson;

                var proofRecordSearch = await _recordService.SearchAsync<ProofRecord>(wallet, new SearchRecordQuery { { "myDid", didOrKey } }, null, 1);
            if (!proofRecordSearch.Any())
                throw new Exception($"Can't find proof record");
            var proofRecord = proofRecordSearch.Single();

            proofRecord.ProofJson = proofJson;
            await proofRecord.TriggerAsync(ProofTrigger.Accept);

            return proofRecord.GetId();
        }

        public async Task<string> StoreProofRequestAsync(Wallet wallet, ProofRequest proofRequest)
        {
            var (didOrKey, _) = MessageUtils.ParseMessageType(proofRequest.Type);

            var connectionSearch = await _connectionService.ListAsync(wallet, new SearchRecordQuery { { "myDid", didOrKey } });
            if (!connectionSearch.Any())
                throw new Exception($"Can't find connection record for type {proofRequest.Type}");
            var connection = connectionSearch.First();

            var (requestDetails, _) = await _messageSerializer.UnpackSealedAsync<ProofRequestDetails>(
                proofRequest.Content,
                wallet, await Did.KeyForLocalDidAsync(wallet, connection.MyDid));
            var requestJson = requestDetails.ProofRequestJson;

            var offer = JObject.Parse(requestJson);
            var nonce = offer["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var proofRecord = new ProofRecord()
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

        private async Task<string> GetSchemasForProof(Wallet wallet, List<string> schemaIds)
        {
            var allSchemas = await _schemaService.ListSchemasAsync(wallet);

            var schemas = allSchemas.Where(_ => schemaIds.Contains(_.SchemaId));

            JObject result = new JObject();

            foreach (var schema in schemas)
                result.Add(schema.SchemaId, schema.SchemaJson);

            return result.ToString();
        }

        private async Task<string> GetCredentialDefsForProof(Wallet wallet, List<string> credentialDefIds)
        {
            var allCredentials = await _credentialService.ListAsync(wallet);

            var credentials = allCredentials.Where(_ => credentialDefIds.Contains(_.GetId()));

            JObject result = new JObject();

            foreach (var credential in credentials)
                result.Add(credential.GetId(), credential.CredentialJson);

            return result.ToString();
        }

        private async Task<string> GenerateProof(Wallet wallet, string requestJson, string masterSecret)
        {
            //Result arrays
            List<(string credId, string credVal, string credRef)> results = new List<(string credId, string credVal, string credRef)>();
            List<string> schemaIds = new List<string>();
            List<string> credDefIds = new List<string>();

            //Create a credentials search based on the proof request
            var credentialsSearch = await AnonCreds.ProverSearchCredentialsForProofRequestAsync(wallet, requestJson);

            //Fetch the attributes and their referents from the proof request
            Dictionary<string, string> attribsAndRefs = ProofUtils.GetReferentsAndAttributes(requestJson);

            //For each requested attribute check the credential search for a match
            foreach (var attribsAndRef in attribsAndRefs)
            {
                //Fetch just one credential from the list
                var result = await credentialsSearch.NextAsync(1, attribsAndRef.Key);

                if (!String.IsNullOrEmpty(result))
                    continue;

                var credArray = JArray.Parse(result);

                if (credArray.Count == 0)
                    continue;

                //Get the credential id and value from the search result
                var credId = credArray[0]["cred_info"]["referent"].ToString();
                var credVal = credArray[0]["cred_info"]["attrs"].ToString();

                //Add the result with the referent to the result array
                results.Add((credId: credId, credVal: credVal, credRef: attribsAndRef.Key));

                //Fetch the schema and cred def id for the current credential, note this list will have duplicates
                schemaIds.Add(credArray[0]["cred_info"]["schema_id"].ToString());
                credDefIds.Add(credArray[0]["cred_info"]["cred_def_id"].ToString());
            }

            //Format the resulting fetched credentials into the request credentials parameter for the CreateProof method
            var requestedCredentials = ProofUtils.GenerateRequestedCredentials(results);

            //Format the schemas in the required format for the CreateProof method
            var schemas = await GetSchemasForProof(wallet, schemaIds.Distinct().ToList());

            //Format the credential definitions in the required format for the CreateProof method
            var credentialDefs = await GetCredentialDefsForProof(wallet, credDefIds.Distinct().ToList());

            return await AnonCreds.ProverCreateProofAsync(wallet, requestJson, requestedCredentials, masterSecret, schemas, credentialDefs, "");
        }

        //TODO add support for self attested credentials
        public async Task<Proof> CreateProof(Wallet wallet, string proofRequestId)
        {
            var request = await _recordService.GetAsync<ProofRecord>(wallet, proofRequestId);
            var connection = await _connectionService.GetAsync(wallet, request.ConnectionId);

            var provisioningRecord = await _provisioningService.GetProvisioningAsync(wallet);

            var proofJson = await GenerateProof(wallet, request.RequestJson, provisioningRecord.MasterSecretId);

            request.ProofJson = proofJson;
            await _recordService.UpdateAsync(wallet, request);

            var proof = await _messageSerializer.PackSealedAsync<Proof>(
                new ProofDetails() { ProofJson = proofJson },
                wallet,
                connection.MyVk,
                connection.TheirVk);
            proof.Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.DisclosedProof);

            return proof;
        }

        public async Task AcceptProofRequestAsync(Wallet wallet, string proofRequestId)
        {
            var request = await _recordService.GetAsync<ProofRecord>(wallet, proofRequestId);
            var connection = await _connectionService.GetAsync(wallet, request.ConnectionId);

            var proof = await CreateProof(wallet, proofRequestId);

            var proofRecord = await _recordService.GetAsync<ProofRecord>(wallet, proofRequestId);

            await proofRecord.TriggerAsync(ProofTrigger.Accept);
            await _recordService.UpdateAsync(wallet, proofRecord);

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

        public Task<bool> VerifyProofAsync(Wallet wallet, string proofRecId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetProofs(Wallet wallet)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetProof(Wallet wallet, string proofRecId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetProofRequests(Wallet wallet)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetProofRequest(Wallet wallet, string proofRecId)
        {
            throw new NotImplementedException();
        }
    }
}
