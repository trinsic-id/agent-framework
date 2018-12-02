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

            var connection = new ConnectionRecord();
            connection.ConnectionId = connectionId;
            connection.Tags[TagConstants.ConnectionKey] = connectionKey;
            connection.Tags[TagConstants.MyKey] = connectionKey;

            if (config.AutoAcceptConnection)
                connection.Tags.Add(TagConstants.AutoAcceptConnection, "true");

            connection.Alias = config.TheirAlias;
            if (!string.IsNullOrEmpty(config.TheirAlias.Name))
                connection.Tags.Add(TagConstants.Alias, config.TheirAlias.Name);

            foreach (var tag in config.Tags)
                connection.Tags[tag.Key] = tag.Value;

            await RecordService.AddAsync(wallet, connection);

            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet) ??
                               throw new AgentFrameworkException(ErrorCode.RecordNotFound,
                                   "Provisioning record not found");

            return new ConnectionInvitationMessage
            {
                Endpoint = provisioning.Endpoint,
                ConnectionKey = connectionKey,
                Name = config.MyAlias.Name ?? provisioning.Owner.Name,
                ImageUrl = config.MyAlias.ImageUrl ?? provisioning.Owner.ImageUrl
            };
        }

        /// <inheritdoc />
        public virtual async Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitationMessage invitation)
        {
            Logger.LogInformation(LoggingEvents.AcceptInvitation, "Key {0}, Endpoint {1}",
                invitation.ConnectionKey, invitation.Endpoint.Uri);

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            var connection = new ConnectionRecord
            {
                Endpoint = invitation.Endpoint,
                MyDid = my.Did,
                MyVk = my.VerKey,
                ConnectionId = Guid.NewGuid().ToString().ToLowerInvariant()
            };
            connection.Tags.Add(TagConstants.MyDid, my.Did);

            if (!string.IsNullOrEmpty(invitation.Name) || !string.IsNullOrEmpty(invitation.ImageUrl))
            {
                connection.Alias = new ConnectionAlias
                {
                    Name = invitation.Name,
                    ImageUrl = invitation.ImageUrl
                };

                if (string.IsNullOrEmpty(invitation.Name))
                    connection.Tags.Add(TagConstants.Alias, invitation.Name);
            }

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await RecordService.AddAsync(wallet, connection);

            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            var msg = new ConnectionRequestMessage
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = provisioning.Endpoint
            };
            
            await RouterService.SendAsync(wallet, msg, connection);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task<string> ProcessRequestAsync(Wallet wallet, ConnectionRequestMessage request, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.ProcessConnectionRequest, "Key {0}", request.Verkey);
            
            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            await Did.StoreTheirDidAsync(wallet, new {did = request.Did, verkey = request.Verkey}.ToJson());

            connection.Endpoint = request.Endpoint;
            connection.TheirDid = request.Did;
            connection.TheirVk = request.Verkey;
            connection.MyDid = my.Did;
            connection.MyVk = my.VerKey;
            connection.Tags[TagConstants.MyDid] = my.Did;
            connection.Tags[TagConstants.MyKey] = my.VerKey;
            connection.Tags[TagConstants.TheirDid] = request.Did;
            connection.Tags[TagConstants.TheirKey] = request.Verkey;

            await RecordService.UpdateAsync(wallet, connection);

            if (connection.Tags.Any(_ => _.Key == TagConstants.AutoAcceptConnection && _.Value == "true"))
                await AcceptRequestAsync(wallet, connection.ConnectionId);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task ProcessResponseAsync(Wallet wallet, ConnectionResponseMessage response, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {1}", connection.MyDid);

            await connection.TriggerAsync(ConnectionTrigger.Response);

            await Did.StoreTheirDidAsync(wallet,
                new { did = response.Did, verkey = response.Verkey }.ToJson());

            await Pairwise.CreateAsync(wallet, response.Did, connection.MyDid,
                response.Endpoint.ToJson());

            connection.TheirDid = response.Did;
            connection.TheirVk = response.Verkey;
            connection.Tags[TagConstants.TheirDid] = response.Did;
            connection.Tags[TagConstants.TheirKey] = response.Verkey;

            if (response.Endpoint != null)
                connection.Endpoint = response.Endpoint;

            await RecordService.UpdateAsync(wallet, connection);
        }

        /// <inheritdoc />
        public virtual async Task AcceptRequestAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionRequest, "ConnectionId {0}", connectionId);

            var connection = await GetAsync(wallet, connectionId) ??
                             throw new AgentFrameworkException(ErrorCode.RecordNotFound,
                                 "Connection record not found");

            await connection.TriggerAsync(ConnectionTrigger.Request);

            await Pairwise.CreateAsync(wallet, connection.TheirDid, connection.MyDid, connection.Endpoint.ToJson());
            await RecordService.UpdateAsync(wallet, connection);

            // Send back response message
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            var response = new ConnectionResponseMessage
            {
                Did = connection.MyDid,
                Endpoint = provisioning.Endpoint,
                Verkey = connection.MyVk
            };

            await RouterService.SendAsync(wallet, response, connection);
        }

        /// <inheritdoc />
        public virtual Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.GetConnection, "ConnectionId {0}", connectionId);

            return RecordService.GetAsync<ConnectionRecord>(wallet, connectionId);
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

            if ((await RecordService.GetAsync<ConnectionRecord>(wallet, connectionId)) == null)
                return true;

            return await RecordService.DeleteAsync<ConnectionRecord>(wallet, connectionId);
        }
    }
}