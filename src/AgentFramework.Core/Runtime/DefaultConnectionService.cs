using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Cache;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Attachments;
using AgentFramework.Core.Decorators.Signature;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Dids;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PairwiseApi;
using Microsoft.Extensions.Logging;
using ConnectionState = AgentFramework.Core.Models.Records.ConnectionState;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultConnectionService : IConnectionService
    {
        /// <summary>
        /// The event aggregator.
        /// </summary>
        protected readonly IEventAggregator EventAggregator;
        /// <summary>
        /// The record service
        /// </summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>
        /// The provisioning service
        /// </summary>
        protected readonly IProvisioningService ProvisioningService;
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger<DefaultConnectionService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConnectionService"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="recordService">The record service.</param>
        /// <param name="provisioningService">The provisioning service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultConnectionService(
            IEventAggregator eventAggregator,
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            ILogger<DefaultConnectionService> logger)
        {
            EventAggregator = eventAggregator;
            ProvisioningService = provisioningService;
            Logger = logger;
            RecordService = recordService;
        }

        /// <inheritdoc />
        public virtual async Task<(ConnectionInvitationMessage, ConnectionRecord)> CreateInvitationAsync(IAgentContext agentContext,
            InviteConfiguration config = null)
        {
            var connectionId = !string.IsNullOrEmpty(config?.ConnectionId)
                ? config.ConnectionId
                : Guid.NewGuid().ToString();

            config = config ?? new InviteConfiguration();

            Logger.LogInformation(LoggingEvents.CreateInvitation, "ConnectionId {0}", connectionId);

            var connectionKey = await Crypto.CreateKeyAsync(agentContext.Wallet, "{}");

            var connection = new ConnectionRecord {Id = connectionId};
            connection.SetTag(TagConstants.ConnectionKey, connectionKey);

            if (config.AutoAcceptConnection)
                connection.SetTag(TagConstants.AutoAcceptConnection, "true");

            connection.MultiPartyInvitation = config.MultiPartyInvitation;

            if (!config.MultiPartyInvitation)
            {
                connection.Alias = config.TheirAlias;
                if (!string.IsNullOrEmpty(config.TheirAlias.Name))
                    connection.SetTag(TagConstants.Alias, config.TheirAlias.Name);
            }

            foreach (var tag in config.Tags)
                connection.SetTag(tag.Key, tag.Value);

            var provisioning = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            await RecordService.AddAsync(agentContext.Wallet, connection);

            return (new ConnectionInvitationMessage
                    {
                        ServiceEndpoint = provisioning.Endpoint.Uri,
                        RoutingKeys = provisioning.Endpoint.Verkey != null ? new[] {provisioning.Endpoint.Verkey} : null,
                        RecipientKeys = new[] {connectionKey},
                        Label = config.MyAlias.Name ?? provisioning.Owner.Name,
                        ImageUrl = config.MyAlias.ImageUrl ?? provisioning.Owner.ImageUrl
                    }, connection);
        }

        /// <inheritdoc />
        public async Task RevokeInvitationAsync(IAgentContext agentContext, string invitationId)
        {
            var connection = await GetAsync(agentContext, invitationId);

            if (connection.State != ConnectionState.Invited)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Invited}', found '{connection.State}'");

            await RecordService.DeleteAsync<ConnectionRecord>(agentContext.Wallet, invitationId);
        }

        /// <inheritdoc />
        public virtual async Task<(ConnectionRequestMessage, ConnectionRecord)> CreateRequestAsync(IAgentContext agentContext, ConnectionInvitationMessage invitation)
        {
            Logger.LogInformation(LoggingEvents.AcceptInvitation, "Key {0}, Endpoint {1}",
                invitation.RecipientKeys[0], invitation.ServiceEndpoint);

            var my = await Did.CreateAndStoreMyDidAsync(agentContext.Wallet, "{}");

            var connection = new ConnectionRecord
            {
                Endpoint = new AgentEndpoint(invitation.ServiceEndpoint, null, invitation.RoutingKeys != null && invitation.RoutingKeys.Count != 0 ? invitation.RoutingKeys[0] : null),
                MyDid = my.Did,
                MyVk = my.VerKey,
                Id = Guid.NewGuid().ToString().ToLowerInvariant()
            };

            if (!string.IsNullOrEmpty(invitation.Label) || !string.IsNullOrEmpty(invitation.ImageUrl))
            {
                connection.Alias = new ConnectionAlias
                {
                    Name = invitation.Label,
                    ImageUrl = invitation.ImageUrl
                };

                if (string.IsNullOrEmpty(invitation.Label))
                    connection.SetTag(TagConstants.Alias, invitation.Label);
            }

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            
            var provisioning = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);
            var request = new ConnectionRequestMessage
            {
                Connection = new Connection {Did = connection.MyDid, DidDoc = connection.MyDidDoc(provisioning)},
                Label = provisioning.Owner?.Name,
                ImageUrl = provisioning.Owner?.ImageUrl
            };

            // also set image as attachment
            if (provisioning.Owner?.ImageUrl != null)
            {
                request.AddAttachment(new Attachment
                {
                    Nickname = "profile-image",
                    Content = new AttachmentContent {Links = new[] {provisioning.Owner.ImageUrl}}
                });
            }
            
            await RecordService.AddAsync(agentContext.Wallet, connection);

            return (request,
                    connection);
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessRequestAsync(IAgentContext agentContext, ConnectionRequestMessage request)
        {
            Logger.LogInformation(LoggingEvents.ProcessConnectionRequest, "Did {0}", request.Connection.Did);

            var my = await Did.CreateAndStoreMyDidAsync(agentContext.Wallet, "{}");

            //TODO throw exception or a problem report if the connection request features a did doc that has no indy agent did doc convention featured
            //i.e there is no way for this agent to respond to messages. And or no keys specified
            await Did.StoreTheirDidAsync(agentContext.Wallet, new { did = request.Connection.Did, verkey = request.Connection.DidDoc.Keys[0].PublicKeyBase58 }.ToJson());

            if (request.Connection.DidDoc.Services[0] is IndyAgentDidDocService service)
                agentContext.Connection.Endpoint = new AgentEndpoint(service.ServiceEndpoint, null, service.RoutingKeys != null && service.RoutingKeys.Count > 0 ? service.RoutingKeys[0] : null);

            agentContext.Connection.TheirDid = request.Connection.Did;
            agentContext.Connection.TheirVk = request.Connection.DidDoc.Keys[0].PublicKeyBase58;
            agentContext.Connection.MyDid = my.Did;
            agentContext.Connection.MyVk = my.VerKey;

            agentContext.Connection.SetTag(TagConstants.LastThreadId, request.Id);
            
            if (agentContext.Connection.Alias == null)
                agentContext.Connection.Alias = new ConnectionAlias();

            if (!string.IsNullOrEmpty(request.Label) && string.IsNullOrEmpty(agentContext.Connection.Alias.Name))
                agentContext.Connection.Alias.Name = request.Label;

            if (!string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(agentContext.Connection.Alias.ImageUrl))
                agentContext.Connection.Alias.ImageUrl = request.ImageUrl;
            
            if (!agentContext.Connection.MultiPartyInvitation)
            {
                await agentContext.Connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
                await RecordService.UpdateAsync(agentContext.Wallet, agentContext.Connection);

                EventAggregator.Publish(new ServiceMessageProcessingEvent
                {
                    RecordId = agentContext.Connection.Id,
                    MessageType = request.Type,
                    ThreadId = request.GetThreadId()
                });

                return agentContext.Connection.Id;
            }

            var newConnection = agentContext.Connection.DeepCopy();
            newConnection.Id = Guid.NewGuid().ToString();

            await newConnection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await RecordService.AddAsync(agentContext.Wallet, newConnection);

            agentContext.Connection = newConnection;

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = newConnection.Id,
                MessageType = request.Type,
                ThreadId = request.GetThreadId()
            });

            return newConnection.Id;
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessResponseAsync(IAgentContext agentContext, ConnectionResponseMessage response)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {1}", agentContext.Connection.MyDid);

            //TODO throw exception or a problem report if the connection request features a did doc that has no indy agent did doc convention featured
            //i.e there is no way for this agent to respond to messages. And or no keys specified
            var connection = SignatureUtils.UnpackAndVerifyData<Connection>(response.ConnectionSig);

            await Did.StoreTheirDidAsync(agentContext.Wallet,
                new { did = connection.Did, verkey = connection.DidDoc.Keys[0].PublicKeyBase58 }.ToJson());

            await Pairwise.CreateAsync(agentContext.Wallet, connection.Did, agentContext.Connection.MyDid,
                connection.DidDoc.Services[0].ServiceEndpoint);

            agentContext.Connection.TheirDid = connection.Did;
            agentContext.Connection.TheirVk = connection.DidDoc.Keys[0].PublicKeyBase58;

            agentContext.Connection.SetTag(TagConstants.LastThreadId, response.GetThreadId());

            if (connection.DidDoc.Services[0] is IndyAgentDidDocService service)
                agentContext.Connection.Endpoint = new AgentEndpoint(service.ServiceEndpoint, null, service.RoutingKeys != null && service.RoutingKeys.Count > 0 ? service.RoutingKeys[0] : null);

            await agentContext.Connection.TriggerAsync(ConnectionTrigger.Response);
            await RecordService.UpdateAsync(agentContext.Wallet, agentContext.Connection);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = agentContext.Connection.Id,
                MessageType = response.Type,
                ThreadId = response.GetThreadId()
            });

            return agentContext.Connection.Id;
        }

        /// <inheritdoc />
        public virtual async Task<(ConnectionResponseMessage, ConnectionRecord)> CreateResponseAsync(IAgentContext agentContext, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionRequest, "ConnectionId {0}", connectionId);

            var connection = await GetAsync(agentContext, connectionId);

            if (connection.State != ConnectionState.Negotiating)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Negotiating}', found '{connection.State}'");

            await Pairwise.CreateAsync(agentContext.Wallet, connection.TheirDid, connection.MyDid, connection.Endpoint.ToJson());

            await connection.TriggerAsync(ConnectionTrigger.Request);
            await RecordService.UpdateAsync(agentContext.Wallet, connection);

            // Send back response message
            var provisioning = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            var connectionData = new Connection
            {
                Did = connection.MyDid,
                DidDoc = connection.MyDidDoc(provisioning)
            };

            var sigData = await SignatureUtils.SignData(agentContext, connectionData, connection.GetTag(TagConstants.ConnectionKey));
            var threadId = connection.GetTag(TagConstants.LastThreadId);

            var response = new ConnectionResponseMessage { ConnectionSig = sigData };
            response.ThreadFrom(threadId);
            
            return (response,connection);
        }

        /// <inheritdoc />
        public virtual async Task<ConnectionRecord> GetAsync(IAgentContext agentContext, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.GetConnection, "ConnectionId {0}", connectionId);

            var record = await RecordService.GetAsync<ConnectionRecord>(agentContext.Wallet, connectionId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Connection record not found");

            return record;
        }

        /// <inheritdoc />
        public virtual Task<List<ConnectionRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null,
            int count = 100)
        {
            Logger.LogInformation(LoggingEvents.ListConnections, "List Connections");

            return RecordService.SearchAsync<ConnectionRecord>(agentContext.Wallet, query, null, count);
        }

        /// <inheritdoc />
        public virtual async Task<bool> DeleteAsync(IAgentContext agentContext, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.DeleteConnection, "ConnectionId {0}", connectionId);
            
            return await RecordService.DeleteAsync<ConnectionRecord>(agentContext.Wallet, connectionId);
        }
    }
}