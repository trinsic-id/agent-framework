using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultCredentialService : ICredentialService
    {
        /// <summary>
        /// The event aggregator.
        /// </summary>
        protected readonly IEventAggregator EventAggregator;

        /// <summary>
        /// The ledger service
        /// </summary>
        protected readonly ILedgerService LedgerService;
        /// <summary>
        /// The connection service
        /// </summary>
        protected readonly IConnectionService ConnectionService;
        /// <summary>
        /// The record service
        /// </summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>
        /// The schema service
        /// </summary>
        protected readonly ISchemaService SchemaService;
        /// <summary>
        /// The tails service
        /// </summary>
        protected readonly ITailsService TailsService;
        /// <summary>
        /// The provisioning service
        /// </summary>
        protected readonly IProvisioningService ProvisioningService;
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger<DefaultCredentialService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCredentialService"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="ledgerService">The ledger service.</param>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="recordService">The record service.</param>
        /// <param name="schemaService">The schema service.</param>
        /// <param name="tailsService">The tails service.</param>
        /// <param name="provisioningService">The provisioning service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultCredentialService(
            IEventAggregator eventAggregator,
            ILedgerService ledgerService,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            ISchemaService schemaService,
            ITailsService tailsService,
            IProvisioningService provisioningService,
            ILogger<DefaultCredentialService> logger)
        {
            EventAggregator = eventAggregator;
            LedgerService = ledgerService;
            ConnectionService = connectionService;
            RecordService = recordService;
            SchemaService = schemaService;
            TailsService = tailsService;
            ProvisioningService = provisioningService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task<CredentialRecord> GetAsync(IAgentContext agentContext, string credentialId)
        {
            var record = await RecordService.GetAsync<CredentialRecord>(agentContext.Wallet, credentialId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Credential record not found");

            return record;
        }
        
        /// <inheritdoc />
        public virtual Task<List<CredentialRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100) =>
            RecordService.SearchAsync<CredentialRecord>(agentContext.Wallet, query, null, count);

        /// <inheritdoc />
        public virtual async Task<string> ProcessOfferAsync(IAgentContext agentContext, CredentialOfferMessage credentialOffer, ConnectionRecord connection)
        {
            var offerJson = credentialOffer.OfferJson;
            var offer = JObject.Parse(offerJson);
            var definitionId = offer["cred_def_id"].ToObject<string>();
            var schemaId = offer["schema_id"].ToObject<string>();
            var nonce = offer["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                OfferJson = offerJson,
                ConnectionId = connection.Id,
                CredentialDefinitionId = definitionId,
                SchemaId = schemaId,
                State = CredentialState.Offered
            };
            credentialRecord.SetTag(TagConstants.Role, TagConstants.Holder);
            credentialRecord.SetTag(TagConstants.Nonce, nonce);

            await RecordService.AddAsync(agentContext.Wallet, credentialRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = credentialRecord.Id,
                MessageType = credentialOffer.Type,
            });

            return credentialRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<CredentialRequestMessage> AcceptOfferAsync(IAgentContext agentContext, string credentialId,
            Dictionary<string, string> attributeValues = null)
        {
            var credential = await GetAsync(agentContext, credentialId);

            if (credential.State != CredentialState.Offered)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Offered}', found '{credential.State}'");

            //var credentialCopy = credential.DeepCopy();

            var connection = await ConnectionService.GetAsync(agentContext, credential.ConnectionId);
            
            var definition = await LedgerService.LookupDefinitionAsync(agentContext.Pool, credential.CredentialDefinitionId);
            var provisioning = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);
            
            var request = await AnonCreds.ProverCreateCredentialReqAsync(agentContext.Wallet, connection.MyDid, credential.OfferJson,
                definition.ObjectJson, provisioning.MasterSecretId);

            // Update local credential record with new info
            credential.CredentialRequestMetadataJson = request.CredentialRequestMetadataJson;
            
            await credential.TriggerAsync(CredentialTrigger.Request);
            await RecordService.UpdateAsync(agentContext.Wallet, credential);

            return new CredentialRequestMessage
            {
                OfferJson = credential.OfferJson,
                CredentialRequestJson = request.CredentialRequestJson,
                CredentialValuesJson = CredentialUtils.FormatCredentialValues(attributeValues)
            };

            //try
            //{
            //    await MessageService.SendToConnectionAsync(agentContext.Wallet, msg, connection);
            //}
            //catch (Exception e)
            //{
            //    await RecordService.UpdateAsync(agentContext.Wallet, credentialCopy);
            //    throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send credential request message", e);
            //}
        }

        /// <inheritdoc />
        public virtual async Task RejectOfferAsync(IAgentContext agentContext, string credentialId)
        {
            var credential = await GetAsync(agentContext, credentialId);

            if (credential.State != CredentialState.Offered)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Offered}', found '{credential.State}'");

            await credential.TriggerAsync(CredentialTrigger.Reject);
            await RecordService.UpdateAsync(agentContext.Wallet, credential);
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessCredentialAsync(IAgentContext agentContext, CredentialMessage credential, ConnectionRecord connection)
        {
            var offer = JObject.Parse(credential.CredentialJson);
            var definitionId = offer["cred_def_id"].ToObject<string>();
            var schemaId = offer["schema_id"].ToObject<string>();
            var revRegId = offer["rev_reg_id"]?.ToObject<string>();

            // TODO: Replace this with thread lookup
            // Currently, this is unable to process multiple offers and requests reliably
            var credentialSearch =
                await RecordService.SearchAsync<CredentialRecord>(agentContext.Wallet,
                SearchQuery.And(
                    SearchQuery.Equal(nameof(CredentialRecord.SchemaId), schemaId),
                    SearchQuery.Equal(nameof(CredentialRecord.CredentialDefinitionId), definitionId),
                    SearchQuery.Equal(nameof(CredentialRecord.ConnectionId), connection.Id),
                    SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Requested.ToString("G"))
                ), null, 5);

            if (credentialSearch.Count == 0)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Credential record not found");

            var credentialRecord = credentialSearch.Single();
            // TODO: Should resolve if multiple credential records are found

            if (credentialRecord.State != CredentialState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Requested}', found '{credentialRecord.State}'");

            var credentialDefinition = await LedgerService.LookupDefinitionAsync(agentContext.Pool, definitionId);

            string revocationRegistryDefinitionJson = null;
            if (!string.IsNullOrEmpty(revRegId))
            {
                // If credential supports revocation, lookup registry definition
                var revocationRegistry =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(agentContext.Pool, revRegId);
                revocationRegistryDefinitionJson = revocationRegistry.ObjectJson;
            }

            var credentialId = await AnonCreds.ProverStoreCredentialAsync(agentContext.Wallet, null,
                credentialRecord.CredentialRequestMetadataJson,
                credential.CredentialJson, credentialDefinition.ObjectJson, revocationRegistryDefinitionJson);

            credentialRecord.CredentialId = credentialId;
            await credentialRecord.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = credentialRecord.Id,
                MessageType = credential.Type,
            });

            return credentialRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<(CredentialOfferMessage, CredentialRecord)> 
            CreateOfferAsync(IAgentContext agentContext, OfferConfiguration config, string connectionId = null)
        {
            Logger.LogInformation(LoggingEvents.CreateCredentialOffer, "DefinitionId {0}, IssuerDid {1}",
                config.CredentialDefinitionId, config.IssuerDid);

            if (!config.MultiPartyOffer && !string.IsNullOrEmpty(connectionId))
            {
                var connection = await ConnectionService.GetAsync(agentContext, connectionId);

                if (connection.State != ConnectionState.Connected)
                    throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                        $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");
            }

            var offerJson =
                await AnonCreds.IssuerCreateCredentialOfferAsync(agentContext.Wallet, config.CredentialDefinitionId);
            var nonce = JObject.Parse(offerJson)["nonce"].ToObject<string>();

            // Write offer record to local wallet
            var credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                CredentialDefinitionId = config.CredentialDefinitionId,
                OfferJson = offerJson,
                MultiPartyOffer = config.MultiPartyOffer,
                ValuesJson = CredentialUtils.FormatCredentialValues(config.CredentialAttributeValues),
                State = CredentialState.Offered,
            };

            if (!config.MultiPartyOffer)
                credentialRecord.ConnectionId = connectionId;

            credentialRecord.SetTag(TagConstants.Nonce, nonce);
            credentialRecord.SetTag(TagConstants.Role, TagConstants.Issuer);

            if (!string.IsNullOrEmpty(config.IssuerDid))
                credentialRecord.Tags.Add(TagConstants.IssuerDid, config.IssuerDid);

            if (config.Tags != null)
                foreach (var tag in config.Tags)
                {
                    if (!credentialRecord.Tags.Keys.Contains(tag.Key))
                        credentialRecord.Tags.Add(tag.Key, tag.Value);
                }

            await RecordService.AddAsync(agentContext.Wallet, credentialRecord);

            return (new CredentialOfferMessage {OfferJson = offerJson}, credentialRecord);
        }

        /// <inheritdoc />
        public async Task RevokeCredentialOfferAsync(IAgentContext agentContext, string offerId)
        {
            var credentialRecord = await GetAsync(agentContext, offerId);

            if (credentialRecord.State != CredentialState.Offered)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Offered}', found '{credentialRecord.State}'");

            await RecordService.DeleteAsync<ConnectionRecord>(agentContext.Wallet, offerId);
        }

        ///// <inheritdoc />
        //public virtual async Task<string> SendOfferAsync(IAgentContext agentContext, string connectionId, OfferConfiguration config)
        //{
        //    Logger.LogInformation(LoggingEvents.SendCredentialOffer, "DefinitionId {0}, ConnectionId {1}, IssuerDid {2}",
        //        config.CredentialDefinitionId, connectionId, config.IssuerDid);

        //    var connection = await ConnectionService.GetAsync(agentContext, connectionId);

        //    if (connection.State != ConnectionState.Connected)
        //        throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
        //            $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

        //    (var offer, string id) = await CreateOfferAsync(agentContext, config);

        //    try
        //    {
        //        await MessageService.SendToConnectionAsync(agentContext.Wallet, offer, connection);
        //    }
        //    catch (Exception e)
        //    {
        //        await RecordService.DeleteAsync<CredentialRecord>(agentContext.Wallet, id);
        //        throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send credential offer message", e);
        //    }

        //    return id;
        //}

        /// <inheritdoc />
        public virtual async Task<string> ProcessCredentialRequestAsync(IAgentContext agentContext, CredentialRequestMessage credentialRequest, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.StoreCredentialRequest, "Type {0},", credentialRequest.Type);
           
            var request = JObject.Parse(credentialRequest.OfferJson);
            var nonce = request["nonce"].ToObject<string>();

            var query = SearchQuery.Equal(TagConstants.Nonce , nonce);
            var credentialSearch = await RecordService.SearchAsync<CredentialRecord>(agentContext.Wallet, query, null, 5);

            if (credentialSearch.Count == 0)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Credential record not found");

            var credential = credentialSearch.Single(); // TODO: Use threading

            if (credential.State != CredentialState.Offered)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Offered}', found '{credential.State}'");

            if (!string.IsNullOrEmpty(credentialRequest.CredentialValuesJson) && JObject.Parse(credentialRequest.CredentialValuesJson).Count != 0)
                    credential.ValuesJson = credentialRequest.CredentialValuesJson;

            credential.RequestJson = credentialRequest.CredentialRequestJson;
            credential.ConnectionId = connection.Id;

            if (!credential.MultiPartyOffer)
            {
                await credential.TriggerAsync(CredentialTrigger.Request);
                await RecordService.UpdateAsync(agentContext.Wallet, credential);

                EventAggregator.Publish(new ServiceMessageProcessingEvent
                {
                    RecordId = credential.Id,
                    MessageType = credentialRequest.Type,
                });

                return credential.Id;
            }

            var newCredential = credential.DeepCopy();
            newCredential.Id = Guid.NewGuid().ToString();
            await credential.TriggerAsync(CredentialTrigger.Request);
            await RecordService.AddAsync(agentContext.Wallet, newCredential);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = newCredential.Id,
                MessageType = credentialRequest.Type,
            });

            return newCredential.Id;
        }

        /// <inheritdoc />
        public virtual async Task RejectCredentialRequestAsync(IAgentContext agentContext, string credentialId)
        {
            var credential = await GetAsync(agentContext, credentialId);

            if (credential.State != CredentialState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Requested}', found '{credential.State}'");

            await credential.TriggerAsync(CredentialTrigger.Reject);
            await RecordService.UpdateAsync(agentContext.Wallet, credential);
        }

        /// <inheritdoc />
        public virtual Task<CredentialMessage> IssueCredentialAsync(IAgentContext agentContext, string issuerDid, string credentialId)
        {
            return IssueCredentialAsync(agentContext, issuerDid, credentialId, null);
        }

        /// <inheritdoc />
        public virtual async Task<CredentialMessage> IssueCredentialAsync(IAgentContext agentContext, string issuerDid, string credentialId,
           Dictionary<string, string> values)
        {
            var credential = await GetAsync(agentContext, credentialId);

            if (credential.State != CredentialState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Requested}', found '{credential.State}'");

            //var credentialCopy = credential.DeepCopy();

            if (values != null && values.Count > 0)
                credential.ValuesJson = CredentialUtils.FormatCredentialValues(values);

            var definitionRecord =
                await SchemaService.GetCredentialDefinitionAsync(agentContext.Wallet, credential.CredentialDefinitionId);

            var connection = await ConnectionService.GetAsync(agentContext, credential.ConnectionId);

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            string revocationRegistryId = null;
            BlobStorageReader tailsReader = null;
            if (definitionRecord.SupportsRevocation)
            {
                var revocationRecordSearch = await RecordService.SearchAsync<RevocationRegistryRecord>(
                    agentContext.Wallet, SearchQuery.Equal(nameof(RevocationRegistryRecord.CredentialDefinitionId), definitionRecord.Id), null, 5);

                var revocationRecord = revocationRecordSearch.Single(); // TODO: Credential definition can have multiple revocation registries

                revocationRegistryId = revocationRecord.Id;
                tailsReader = await TailsService.OpenTailsAsync(revocationRecord.TailsFile);
            }

            var issuedCredential = await AnonCreds.IssuerCreateCredentialAsync(agentContext.Wallet, credential.OfferJson,
                credential.RequestJson, credential.ValuesJson, revocationRegistryId, tailsReader);

            if (definitionRecord.SupportsRevocation)
            {
                await LedgerService.SendRevocationRegistryEntryAsync(agentContext.Wallet, agentContext.Pool, issuerDid,
                    revocationRegistryId,
                    "CL_ACCUM", issuedCredential.RevocRegDeltaJson);
                credential.CredentialRevocationId = issuedCredential.RevocId;
            }

            await credential.TriggerAsync(CredentialTrigger.Issue);
            await RecordService.UpdateAsync(agentContext.Wallet, credential);

            return new CredentialMessage
            {
                CredentialJson = issuedCredential.CredentialJson,
                RevocationRegistryId = revocationRegistryId
            };
            //try
            //{
            //    await MessageService.SendToConnectionAsync(agentContext.Wallet, msg, connection);
            //}
            //catch (Exception e)
            //{
            //    await RecordService.UpdateAsync(agentContext.Wallet, credentialCopy);
            //    throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send credential request message", e);
            //}
        }

        /// <inheritdoc />
        public virtual async Task RevokeCredentialAsync(IAgentContext agentContext, string credentialId, string issuerDid)
        {
            var credential = await GetAsync(agentContext, credentialId);

            if (credential.State != CredentialState.Issued)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Credential state was invalid. Expected '{CredentialState.Requested}', found '{credential.State}'");

            var definition = await SchemaService.GetCredentialDefinitionAsync(agentContext.Wallet, credential.CredentialDefinitionId);

            // Check if the state machine is valid for revocation
            await credential.TriggerAsync(CredentialTrigger.Revoke);

            var revocationRecordSearch = await RecordService.SearchAsync<RevocationRegistryRecord>(
                agentContext.Wallet, SearchQuery.Equal(nameof(RevocationRegistryRecord.CredentialDefinitionId), definition.Id), null, 5);
            var revocationRecord = revocationRecordSearch.Single(); // TODO: Add support for multiple revocation registries

            // Revoke the credential
            var tailsReader = await TailsService.OpenTailsAsync(revocationRecord.TailsFile);
            var revocRegistryDeltaJson = await AnonCreds.IssuerRevokeCredentialAsync(agentContext.Wallet, tailsReader,
                revocationRecord.Id, credential.CredentialRevocationId);

            // Write the delta state on the ledger for the corresponding revocation registry
            await LedgerService.SendRevocationRegistryEntryAsync(agentContext.Wallet, agentContext.Pool, issuerDid,
                revocationRecord.Id,
                "CL_ACCUM", revocRegistryDeltaJson);

            // Update local credential record
            await RecordService.UpdateAsync(agentContext.Wallet, credential);
        }
    }
}