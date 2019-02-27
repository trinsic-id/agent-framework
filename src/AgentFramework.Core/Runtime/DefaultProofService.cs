using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Runtime
{
    /// <summary>
    /// Proof Service
    /// </summary>
    /// <seealso cref="AgentFramework.Core.Contracts.IProofService" />
    public class DefaultProofService : IProofService
    {
        /// <summary>
        /// The event aggregator
        /// </summary>
        protected readonly IEventAggregator EventAggregator;
        /// <summary>
        /// The message service
        /// </summary>
        protected readonly IMessageService MessageService;
        /// <summary>
        /// The connection service
        /// </summary>
        protected readonly IConnectionService ConnectionService;
        /// <summary>
        /// The record service
        /// </summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>
        /// The provisioning service
        /// </summary>
        protected readonly IProvisioningService ProvisioningService;
        /// <summary>
        /// The ledger service
        /// </summary>
        protected readonly ILedgerService LedgerService;
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger<DefaultProofService> Logger;
        /// <summary>
        /// The tails service
        /// </summary>
        protected readonly ITailsService TailsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultProofService"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="messageService">The message service.</param>
        /// <param name="recordService">The record service.</param>
        /// <param name="provisioningService">The provisioning service.</param>
        /// <param name="ledgerService">The ledger service.</param>
        /// <param name="tailsService">The tails service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultProofService(
            IEventAggregator eventAggregator,
            IConnectionService connectionService,
            IMessageService messageService,
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            ILedgerService ledgerService,
            ITailsService tailsService,
            ILogger<DefaultProofService> logger)
        {
            EventAggregator = eventAggregator;
            TailsService = tailsService;
            ConnectionService = connectionService;
            MessageService = messageService;
            RecordService = recordService;
            ProvisioningService = provisioningService;
            LedgerService = ledgerService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task SendProofRequestAsync(IAgentContext agentContext, string connectionId, ProofRequest proofRequest)
        {
            Logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await ConnectionService.GetAsync(agentContext, connectionId);

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            var (msg, id) = await CreateProofRequestAsync(agentContext, connectionId, proofRequest);

            try
            {
                await MessageService.SendAsync(agentContext.Wallet, msg, connection);
            }
            catch (Exception e)
            {
                await RecordService.DeleteAsync<ProofRecord>(agentContext.Wallet, id);
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send proof request message", e);
            }
        }

        /// <inheritdoc />
        public virtual async Task SendProofRequestAsync(IAgentContext agentContext, string connectionId, string proofRequestJson)
        {
            Logger.LogInformation(LoggingEvents.SendProofRequest, "ConnectionId {0}", connectionId);

            var connection = await ConnectionService.GetAsync(agentContext, connectionId);

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            var (msg, id) = await CreateProofRequestAsync(agentContext, connectionId, proofRequestJson);

            try
            {
                await MessageService.SendAsync(agentContext.Wallet, msg, connection);
            }
            catch (Exception e)
            {
                await RecordService.DeleteAsync<ProofRecord>(agentContext.Wallet, id);
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send proof request message", e);
            }
        }

        /// <inheritdoc />
        public virtual async Task<(ProofRequestMessage, string)> CreateProofRequestAsync(IAgentContext agentContext, string connectionId,
            ProofRequest proofRequest)
        {
            if (string.IsNullOrWhiteSpace(proofRequest.Nonce))
                throw new ArgumentNullException(nameof(proofRequest.Nonce), "Nonce must be set.");

            return await CreateProofRequestAsync(agentContext, connectionId, proofRequest.ToJson());
        }

        /// <inheritdoc />
        public virtual async Task<(ProofRequestMessage, string)> CreateProofRequestAsync(IAgentContext agentContext, string connectionId,
            string proofRequestJson)
        {
            Logger.LogInformation(LoggingEvents.CreateProofRequest, "ConnectionId {0}", connectionId);

            var connection = await ConnectionService.GetAsync(agentContext, connectionId);

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            var proofJobj = JObject.Parse(proofRequestJson);

            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connection.Id,
                RequestJson = proofRequestJson
            };
            proofRecord.SetTag(TagConstants.Nonce, proofJobj["nonce"].ToObject<string>());
            proofRecord.SetTag(TagConstants.Role, TagConstants.Requestor);

            await RecordService.AddAsync(agentContext.Wallet, proofRecord);

            return (new ProofRequestMessage { ProofRequestJson = proofRequestJson }, proofRecord.Id);
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessProofAsync(IAgentContext agentContext, ProofMessage proof)
        {
            var proofJson = proof.ProofJson;

            var proofRecordSearch =
                await RecordService.SearchAsync<ProofRecord>(agentContext.Wallet,
                    SearchQuery.Equal(TagConstants.Nonce, proof.RequestNonce), null, 1);

            var proofRecord = proofRecordSearch.FirstOrDefault() ??
                              throw new AgentFrameworkException(ErrorCode.RecordNotFound,
                                  "Proof record not found");

            if (proofRecord.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof state was invalid. Expected '{ProofState.Requested}', found '{proofRecord.State}'");

            proofRecord.ProofJson = proofJson;
            await proofRecord.TriggerAsync(ProofTrigger.Accept);
            await RecordService.UpdateAsync(agentContext.Wallet, proofRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = proofRecord.Id,
                MessageType = proof.Type,
            });

            return proofRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessProofRequestAsync(IAgentContext agentContext, ProofRequestMessage proofRequest)
        {
            var requestJson = proofRequest.ProofRequestJson;

            var offer = JObject.Parse(requestJson);
            var nonce = offer["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestJson = requestJson,
                ConnectionId = agentContext.Connection.Id,
                State = ProofState.Requested
            };
            proofRecord.SetTag(TagConstants.Nonce, nonce);
            proofRecord.SetTag(TagConstants.Role, TagConstants.Holder);

            await RecordService.AddAsync(agentContext.Wallet, proofRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = proofRecord.Id,
                MessageType = proofRequest.Type,
            });

            return proofRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<ProofMessage> CreateProofAsync(IAgentContext agentContext, string proofRequestId,
            RequestedCredentials requestedCredentials)
        {
            var record = await GetAsync(agentContext, proofRequestId);

            if (record.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof state was invalid. Expected '{ProofState.Requested}', found '{record.State}'");

            var provisioningRecord = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            var credentialObjects = new List<CredentialInfo>();
            foreach (var credId in requestedCredentials.GetCredentialIdentifiers())
            {
                credentialObjects.Add(
                    JsonConvert.DeserializeObject<CredentialInfo>(
                        await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, credId)));
            }

            var schemas = await BuildSchemasAsync(agentContext.Pool,
                credentialObjects
                    .Select(x => x.SchemaId)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(agentContext.Pool,
                credentialObjects
                    .Select(x => x.CredentialDefinitionId)
                    .Distinct());

            var revocationStates = await BuildRevocationStatesAsync(agentContext.Pool,
                credentialObjects,
                requestedCredentials);

            var proofJson = await AnonCreds.ProverCreateProofAsync(agentContext.Wallet, record.RequestJson,
                requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
                revocationStates);

            record.ProofJson = proofJson;
            await record.TriggerAsync(ProofTrigger.Accept);
            await RecordService.UpdateAsync(agentContext.Wallet, record);

            return new ProofMessage
            {
                ProofJson = proofJson,
                RequestNonce = JsonConvert.DeserializeObject<ProofRequest>(record.RequestJson).Nonce
            };
        }

        /// <inheritdoc />
        public virtual async Task<ProofMessage> AcceptProofRequestAsync(IAgentContext agentContext, string proofRequestId,
            RequestedCredentials requestedCredentials)
        {
            var request = await GetAsync(agentContext, proofRequestId);

            if (request.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof record state was invalid. Expected '{ProofState.Requested}', found '{request.State}'");

            var connection = await ConnectionService.GetAsync(agentContext, request.ConnectionId);

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            return await CreateProofAsync(agentContext, proofRequestId, requestedCredentials);
        }

        /// <inheritdoc />
        public virtual async Task RejectProofRequestAsync(IAgentContext agentContext, string proofRequestId)
        {
            var request = await GetAsync(agentContext, proofRequestId);

            if (request.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof record state was invalid. Expected '{ProofState.Requested}', found '{request.State}'");

            await request.TriggerAsync(ProofTrigger.Reject);
            await RecordService.UpdateAsync(agentContext.Wallet, request);
        }

        /// <inheritdoc />
        public virtual async Task<bool> VerifyProofAsync(IAgentContext agentContext, string proofRecId)
        {
            var proofRecord = await GetAsync(agentContext, proofRecId);

            if (proofRecord.State != ProofState.Accepted)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof record state was invalid. Expected '{ProofState.Accepted}', found '{proofRecord.State}'");

            var proofObject = JsonConvert.DeserializeObject<Proof>(proofRecord.ProofJson);

            var schemas = await BuildSchemasAsync(agentContext.Pool,
                proofObject.Identifiers
                    .Select(x => x.SchemaId)
                    .Where(x => x != null)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(agentContext.Pool,
                proofObject.Identifiers
                    .Select(x => x.CredentialDefintionId)
                    .Where(x => x != null)
                    .Distinct());

            var revocationDefinitions = await BuildRevocationRegistryDefinitionsAsync(agentContext.Pool,
                proofObject.Identifiers
                    .Select(x => x.RevocationRegistryId)
                    .Where(x => x != null)
                    .Distinct());

            var revocationRegistries = await BuildRevocationRegistryDetlasAsync(agentContext.Pool,
                proofObject.Identifiers
                    .Where(x => x.RevocationRegistryId != null));

            return await AnonCreds.VerifierVerifyProofAsync(proofRecord.RequestJson, proofRecord.ProofJson, schemas,
                definitions, revocationDefinitions, revocationRegistries);
        }

        /// <inheritdoc />
        public virtual Task<List<ProofRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100) =>
            RecordService.SearchAsync<ProofRecord>(agentContext.Wallet, query, null, count);

        /// <inheritdoc />
        public virtual async Task<ProofRecord> GetAsync(IAgentContext agentContext, string proofRecId)
        {
            Logger.LogInformation(LoggingEvents.GetProofRecord, "ConnectionId {0}", proofRecId);

            var record = await RecordService.GetAsync<ProofRecord>(agentContext.Wallet, proofRecId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Proof record not found");

            return record;
        }
        
        /// <inheritdoc />
        public virtual async Task<List<Credential>> ListCredentialsForProofRequestAsync(IAgentContext agentContext,
            ProofRequest proofRequest, string attributeReferent)
        {
            using (var search =
                await AnonCreds.ProverSearchCredentialsForProofRequestAsync(agentContext.Wallet, proofRequest.ToJson()))
            {
                var searchResult = await search.NextAsync(attributeReferent, 100);
                return JsonConvert.DeserializeObject<List<Credential>>(searchResult);
            }
        }

        #region Private Methods
        private async Task<string> BuildSchemasAsync(Pool pool, IEnumerable<string> schemaIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in schemaIds)
            {
                var ledgerSchema = await LedgerService.LookupSchemaAsync(pool, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildCredentialDefinitionsAsync(Pool pool, IEnumerable<string> credentialDefIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in credentialDefIds)
            {
                var ledgerDefinition = await LedgerService.LookupDefinitionAsync(pool, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerDefinition.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationStatesAsync(Pool pool,
            IEnumerable<CredentialInfo> credentialObjects,
            RequestedCredentials requestedCredentials)
        {
            var allCredentials = new List<RequestedAttribute>();
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
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool,
                        credential.RevocationRegistryId);

                var delta = await LedgerService.LookupRevocationRegistryDeltaAsync(pool,
                    credential.RevocationRegistryId, -1, timestamp);

                var tailsfile = await TailsService.EnsureTailsExistsAsync(pool, credential.RevocationRegistryId);
                var tailsReader = await TailsService.OpenTailsAsync(tailsfile);

                var state = await AnonCreds.CreateRevocationStateAsync(tailsReader, registryDefinition.ObjectJson,
                    delta.ObjectJson, (long)delta.Timestamp, credential.CredentialRevocationId);

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
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool, revocationRegistryId);
                result.Add(revocationRegistryId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }
        #endregion
    }
}