using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
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
        protected readonly IWalletRecordService RecordService;
        protected readonly IRouterService RouterService;
        protected readonly IProvisioningService ProvisioningService;
        protected readonly IMessageSerializer MessageSerializer;
        protected readonly ILogger<DefaultConnectionService> Logger;

        public DefaultConnectionService(
            IWalletRecordService recordService,
            IRouterService routerService,
            IProvisioningService provisioningService,
            IMessageSerializer messageSerializer,
            ILogger<DefaultConnectionService> logger)
        {
            RouterService = routerService;
            ProvisioningService = provisioningService;
            MessageSerializer = messageSerializer;
            Logger = logger;
            RecordService = recordService;
        }

        /// <inheritdoc />
        public virtual async Task<ConnectionInvitationMessage> CreateInvitationAsync(Wallet wallet,
            InviteConfiguration config = null)
        {
            var connectionId = !string.IsNullOrEmpty(config?.ConnectionId)
                ? config.ConnectionId
                : Guid.NewGuid().ToString();

            config = config ?? new InviteConfiguration();

            Logger.LogInformation(LoggingEvents.CreateInvitation, "ConnectionId {0}", connectionId);

            var connectionKey = await Crypto.CreateKeyAsync(wallet, "{}");

            var connection = new ConnectionRecord { Id = connectionId };
            connection.SetTag(TagConstants.ConnectionKey, connectionKey);

            if (config.AutoAcceptConnection)
                connection.SetTag(TagConstants.AutoAcceptConnection, "true");

            connection.Alias = config.TheirAlias;
            if (!string.IsNullOrEmpty(config.TheirAlias.Name))
                connection.SetTag(TagConstants.Alias, config.TheirAlias.Name);

            foreach (var tag in config.Tags)
                connection.SetTag(tag.Key, tag.Value);

            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);

            await RecordService.AddAsync(wallet, connection);

            return new ConnectionInvitationMessage
            {
                Endpoint = provisioning.Services[0],
                ConnectionKey = connectionKey,
                Name = config.MyAlias.Name ?? provisioning.Owner.Name,
                ImageUrl = config.MyAlias.ImageUrl ?? provisioning.Owner.ImageUrl
            };
        }

        /// <inheritdoc />
        public virtual async Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitationMessage invitation)
        {
            Logger.LogInformation(LoggingEvents.AcceptInvitation, "Key {0}, Endpoint {1}",
                invitation.ConnectionKey, invitation.Endpoint.ServiceEndpoint);

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            var connection = new ConnectionRecord
            {
                MyDid = my.Did,
                MyVk = my.VerKey,
                Id = Guid.NewGuid().ToString().ToLowerInvariant()
            };

            connection.Services.Add(invitation.Endpoint);

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
            await RecordService.AddAsync(wallet, connection);

            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            var msg = new ConnectionRequestMessage
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = provisioning.Services[0]
            };

            try
            {
                await RouterService.SendAsync(wallet, msg, connection, invitation.ConnectionKey);
            }
            catch (Exception e)
            {
                await RecordService.DeleteAsync<ConnectionRecord>(wallet, connection.Id);
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send connection request message", e);
            }

            return connection.Id;
        }

        /// <inheritdoc />
        public async Task<string> ProcessRequestAsync(Wallet wallet, ConnectionRequestMessage request, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.ProcessConnectionRequest, "Key {0}", request.Verkey);
            
            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            await Did.StoreTheirDidAsync(wallet, new { did = request.Did, verkey = request.Verkey }.ToJson());

            connection.Services.Add(request.Endpoint);
            connection.TheirDid = request.Did;
            connection.TheirVk = request.Verkey;
            connection.MyDid = my.Did;
            connection.MyVk = my.VerKey;

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await RecordService.UpdateAsync(wallet, connection);

            try
            {
                if (connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                    await AcceptRequestAsync(wallet, connection.Id);
            }
            catch (Exception)
            {
                await RecordService.DeleteAsync<ConnectionRecord>(wallet, connection.Id);
                throw;
            }

            return connection.Id;
        }

        /// <inheritdoc />
        public async Task ProcessResponseAsync(Wallet wallet, ConnectionResponseMessage response, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {1}", connection.MyDid);
            
            await Did.StoreTheirDidAsync(wallet,
                new { did = response.Did, verkey = response.Verkey }.ToJson());

            await Pairwise.CreateAsync(wallet, response.Did, connection.MyDid,
                response.Endpoint.ToJson());

            connection.TheirDid = response.Did;
            connection.TheirVk = response.Verkey;

            await connection.TriggerAsync(ConnectionTrigger.Response);
            await RecordService.UpdateAsync(wallet, connection);
        }

        /// <inheritdoc />
        public virtual async Task AcceptRequestAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionRequest, "ConnectionId {0}", connectionId);

            var connection = await GetAsync(wallet, connectionId);

            if (connection.State != ConnectionState.Negotiating)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Negotiating}', found '{connection.State}'");

            var connectionCopy = connection.DeepCopy();

            await Pairwise.CreateAsync(wallet, connection.TheirDid, connection.MyDid, "{}");

            await connection.TriggerAsync(ConnectionTrigger.Request);
            await RecordService.UpdateAsync(wallet, connection);

            // Send back response message
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            var response = new ConnectionResponseMessage
            {
                Did = connection.MyDid,
                Endpoint = provisioning.Services[0],
                Verkey = connection.MyVk
            };

            try
            {
                await RouterService.SendAsync(wallet, response, connection);
            }
            catch (Exception e)
            {
                await RecordService.UpdateAsync(wallet, connectionCopy);
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send connection response message", e);
            }
        }

        /// <inheritdoc />
        public virtual async Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.GetConnection, "ConnectionId {0}", connectionId);

            var record = await RecordService.GetAsync<ConnectionRecord>(wallet, connectionId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Connection record not found");

            return record;
        }

        /// <inheritdoc />
        public virtual Task<List<ConnectionRecord>> ListAsync(Wallet wallet, ISearchQuery query = null,
            int count = 100)
        {
            Logger.LogInformation(LoggingEvents.ListConnections, "List Connections");

            return RecordService.SearchAsync<ConnectionRecord>(wallet, query, null, count);
        }

        /// <inheritdoc />
        public virtual async Task<bool> DeleteAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.DeleteConnection, "ConnectionId {0}", connectionId);
            
            return await RecordService.DeleteAsync<ConnectionRecord>(wallet, connectionId);
        }
    }
}