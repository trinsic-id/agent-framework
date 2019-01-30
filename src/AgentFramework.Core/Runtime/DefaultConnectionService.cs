using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PairwiseApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultConnectionService : IConnectionService
    {
        /// <summary>
        /// The record service
        /// </summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>
        /// The message service
        /// </summary>
        protected readonly IMessageService MessageService;
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
        /// <param name="recordService">The record service.</param>
        /// <param name="messageService">The message service.</param>
        /// <param name="provisioningService">The provisioning service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultConnectionService(
            IWalletRecordService recordService,
            IMessageService messageService,
            IProvisioningService provisioningService,
            ILogger<DefaultConnectionService> logger)
        {
            MessageService = messageService;
            ProvisioningService = provisioningService;
            Logger = logger;
            RecordService = recordService;
        }

        /// <inheritdoc />
        public virtual async Task<ConnectionInvitationMessage> CreateInvitationAsync(IAgentContext agentContext,
            InviteConfiguration config = null)
        {
            var connectionId = !string.IsNullOrEmpty(config?.ConnectionId)
                ? config.ConnectionId
                : Guid.NewGuid().ToString();

            config = config ?? new InviteConfiguration();

            Logger.LogInformation(LoggingEvents.CreateInvitation, "ConnectionId {0}", connectionId);

            var connectionKey = await Crypto.CreateKeyAsync(agentContext.Wallet, "{}");

            var connection = new ConnectionRecord { Id = connectionId };
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

            return new ConnectionInvitationMessage
            {
                Endpoint = provisioning.Endpoint,
                ConnectionKey = connectionKey,
                Name = config.MyAlias.Name ?? provisioning.Owner.Name,
                ImageUrl = config.MyAlias.ImageUrl ?? provisioning.Owner.ImageUrl
            };
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
        public virtual async Task<ConnectionRequestMessage> AcceptInvitationAsync(IAgentContext agentContext, ConnectionInvitationMessage invitation)
        {
            Logger.LogInformation(LoggingEvents.AcceptInvitation, "Key {0}, Endpoint {1}",
                invitation.ConnectionKey, invitation.Endpoint.Uri);

            var my = await Did.CreateAndStoreMyDidAsync(agentContext.Wallet, "{}");

            var connection = new ConnectionRecord
            {
                Endpoint = invitation.Endpoint,
                MyDid = my.Did,
                MyVk = my.VerKey,
                Id = Guid.NewGuid().ToString().ToLowerInvariant()
            };

            if (!string.IsNullOrEmpty(invitation.Name) || !string.IsNullOrEmpty(invitation.ImageUrl))
            {
                connection.Alias = new ConnectionAlias
                {
                    Name = invitation.Name,
                    ImageUrl = invitation.ImageUrl
                };

                if (string.IsNullOrEmpty(invitation.Name))
                    connection.SetTag(TagConstants.Alias, invitation.Name);
            }

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await RecordService.AddAsync(agentContext.Wallet, connection);

            var provisioning = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);
            return new ConnectionRequestMessage
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = provisioning.Endpoint,
                Name = provisioning.Owner?.Name,
                ImageUrl = provisioning.Owner?.ImageUrl
            };
        }

        /// <inheritdoc />
        public async Task<string> ProcessRequestAsync(IAgentContext agentContext, ConnectionRequestMessage request)
        {
            Logger.LogInformation(LoggingEvents.ProcessConnectionRequest, "Key {0}", request.Verkey);
            
            var my = await Did.CreateAndStoreMyDidAsync(agentContext.Wallet, "{}");

            await Did.StoreTheirDidAsync(agentContext.Wallet, new { did = request.Did, verkey = request.Verkey }.ToJson());

            agentContext.Connection.Endpoint = request.Endpoint;
            agentContext.Connection.TheirDid = request.Did;
            agentContext.Connection.TheirVk = request.Verkey;
            agentContext.Connection.MyDid = my.Did;
            agentContext.Connection.MyVk = my.VerKey;

            agentContext.Connection.Alias = new ConnectionAlias
            {
                Name = request.Name,
                ImageUrl = request.ImageUrl
            };

            if (!agentContext.Connection.MultiPartyInvitation)
            {
                await agentContext.Connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
                await RecordService.UpdateAsync(agentContext.Wallet, agentContext.Connection);
                return agentContext.Connection.Id;
            }

            var newConnection = agentContext.Connection.DeepCopy();
            newConnection.Id = Guid.NewGuid().ToString();
            await newConnection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await RecordService.AddAsync(agentContext.Wallet, newConnection);
            return newConnection.Id;
        }

        /// <inheritdoc />
        public async Task ProcessResponseAsync(IAgentContext agentContext, ConnectionResponseMessage response)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {1}", agentContext.Connection.MyDid);
            
            await Did.StoreTheirDidAsync(agentContext.Wallet,
                new { did = response.Did, verkey = response.Verkey }.ToJson());

            await Pairwise.CreateAsync(agentContext.Wallet, response.Did, agentContext.Connection.MyDid,
                response.Endpoint.ToJson());

            agentContext.Connection.TheirDid = response.Did;
            agentContext.Connection.TheirVk = response.Verkey;

            if (response.Endpoint != null)
                agentContext.Connection.Endpoint = response.Endpoint;

            await agentContext.Connection.TriggerAsync(ConnectionTrigger.Response);
            await RecordService.UpdateAsync(agentContext.Wallet, agentContext.Connection);
        }

        /// <inheritdoc />
        public virtual async Task<ConnectionResponseMessage> AcceptRequestAsync(IAgentContext agentContext, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionRequest, "ConnectionId {0}", connectionId);

            var connection = await GetAsync(agentContext, connectionId);

            if (connection.State != ConnectionState.Negotiating)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Negotiating}', found '{connection.State}'");

            var connectionCopy = connection.DeepCopy();

            await Pairwise.CreateAsync(agentContext.Wallet, connection.TheirDid, connection.MyDid, connection.Endpoint.ToJson());

            await connection.TriggerAsync(ConnectionTrigger.Request);
            await RecordService.UpdateAsync(agentContext.Wallet, connection);

            // Send back response message
            var provisioning = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);
            return new ConnectionResponseMessage
            {
                Did = connection.MyDid,
                Endpoint = provisioning.Endpoint,
                Verkey = connection.MyVk
            };
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