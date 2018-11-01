using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PairwiseApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Messages.Connection;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
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
        public virtual async Task<ConnectionInvitation> CreateInvitationAsync(Wallet wallet,
            DefaultCreateInviteConfiguration config = null)
        {
            var connectionId = !string.IsNullOrEmpty(config?.ConnectionId)
                ? config.ConnectionId
                : Guid.NewGuid().ToString();

            Logger.LogInformation(LoggingEvents.CreateInvitation, "ConnectionId {0}", connectionId);

            var connectionKey = await Crypto.CreateKeyAsync(wallet, "{}");

            var connection = new ConnectionRecord();
            connection.ConnectionId = connectionId;
            connection.Tags.Add(TagConstants.ConnectionKey, connectionKey);

            if (config != null && config.AutoAcceptConnection)
                connection.Tags.Add(TagConstants.AutoAcceptConnection, "true");

            if (config?.TheirAlias != null)
            {
                connection.Alias = config.TheirAlias;
                if (!string.IsNullOrEmpty(config.TheirAlias.Name))
                    connection.Tags.Add(TagConstants.Alias, config.TheirAlias.Name);
            }

            if (config?.Tags != null)
                foreach (var tag in config.Tags)
                {
                    if (!connection.Tags.Keys.Contains(tag.Key))
                        connection.Tags.Add(tag.Key, tag.Value);
                }

            await RecordService.AddAsync(wallet, connection);

            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);

            var invite = new ConnectionInvitation
            {
                Endpoint = provisioning.Endpoint,
                ConnectionKey = connectionKey
            };

            if (!string.IsNullOrEmpty(provisioning.Owner?.Name))
                invite.Name = provisioning.Owner.Name;
            if (!string.IsNullOrEmpty(provisioning.Owner?.ImageUrl))
                invite.ImageUrl = provisioning.Owner.ImageUrl;

            if (!string.IsNullOrEmpty(config?.MyAlias?.Name))
                invite.Name = config.MyAlias.Name;
            if (!string.IsNullOrEmpty(config?.MyAlias?.ImageUrl))
                invite.ImageUrl = config.MyAlias.ImageUrl;

            return invite;
        }

        /// <inheritdoc />
        public virtual async Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitation invitation)
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
            var connectionDetails = new ConnectionDetails
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = provisioning.Endpoint
            };

            var request = await MessageSerializer.PackSealedAsync<ConnectionRequest>(connectionDetails, wallet,
                my.VerKey, invitation.ConnectionKey);
            request.Key = invitation.ConnectionKey;
            request.Type = MessageUtils.FormatKeyMessageType(invitation.ConnectionKey, MessageTypes.ConnectionRequest);

            var forwardMessage = new ForwardToKeyEnvelopeMessage
            {
                Type = MessageUtils.FormatKeyMessageType(invitation.ConnectionKey, MessageTypes.ForwardToKey),
                Content = request.ToJson()
            };

            await RouterService.ForwardAsync(forwardMessage, invitation.Endpoint);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task<string> ProcessRequestAsync(Wallet wallet, ConnectionRequest request)
        {
            Logger.LogInformation(LoggingEvents.StoreConnectionRequest, "Key {0}", request.Key);

            var (didOrKey, _) = MessageUtils.ParseMessageType(request.Type);

            var connectionSearch = await RecordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery {{TagConstants.ConnectionKey, didOrKey}}, null, 1);

            var connection = connectionSearch.Single();

            var (their, theirKey) =
                await MessageSerializer.UnpackSealedAsync<ConnectionDetails>(request.Content, wallet, request.Key);

            if (!their.Verkey.Equals(theirKey)) throw new ArgumentException("Signed and enclosed keys don't match");

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            await Did.StoreTheirDidAsync(wallet, new {did = their.Did, verkey = their.Verkey}.ToJson());

            connection.Endpoint = their.Endpoint;
            connection.TheirDid = their.Did;
            connection.TheirVk = their.Verkey;
            connection.MyDid = my.Did;
            connection.MyVk = my.VerKey;
            connection.Tags[TagConstants.MyDid] = my.Did;
            connection.Tags[TagConstants.TheirDid] = their.Did;

            await RecordService.UpdateAsync(wallet, connection);

            if (connection.Tags.Any(_ => _.Key == TagConstants.AutoAcceptConnection && _.Value == "true"))
                await AcceptRequestAsync(wallet, connection.ConnectionId);

            return connection.GetId();
        }

        /// <inheritdoc />
        public virtual async Task AcceptRequestAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionRequest, "ConnectionId {0}", connectionId);

            var connection = await GetAsync(wallet, connectionId);

            await connection.TriggerAsync(ConnectionTrigger.Request);

            await Pairwise.CreateAsync(wallet, connection.TheirDid, connection.MyDid, connection.Endpoint.ToJson());
            await RecordService.UpdateAsync(wallet, connection);

            // Send back response message
            var provisioning = await ProvisioningService.GetProvisioningAsync(wallet);
            var response = new ConnectionDetails
            {
                Did = connection.MyDid,
                Endpoint = provisioning.Endpoint,
                Verkey = connection.MyVk
            };

            var responseMessage =
                await MessageSerializer.PackSealedAsync<ConnectionResponse>(response, wallet, connection.MyVk,
                    connection.TheirVk);
            responseMessage.Type =
                MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.ConnectionResponse);
            responseMessage.To = connection.TheirDid;

            var forwardMessage = new ForwardEnvelopeMessage
            {
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward),
                Content = responseMessage.ToJson()
            };

            await RouterService.ForwardAsync(forwardMessage, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task ProcessResponseAsync(Wallet wallet, ConnectionResponse response)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {0}", response.To);

            var (didOrKey, _) = MessageUtils.ParseMessageType(response.Type);

            var connectionSearch = await RecordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery {{ TagConstants.MyDid, didOrKey}}, null, 1);

            var connection = connectionSearch.Single();
            await connection.TriggerAsync(ConnectionTrigger.Response);

            var (connectionDetails, _) = await MessageSerializer.UnpackSealedAsync<ConnectionDetails>(response.Content,
                wallet, connection.MyVk);

            await Did.StoreTheirDidAsync(wallet,
                new {did = connectionDetails.Did, verkey = connectionDetails.Verkey}.ToJson());

            await Pairwise.CreateAsync(wallet, connectionDetails.Did, connection.MyDid,
                connectionDetails.Endpoint.ToJson());

            connection.TheirDid = connectionDetails.Did;
            connection.TheirVk = connectionDetails.Verkey;

            if (connectionDetails.Endpoint != null)
                connection.Endpoint = connectionDetails.Endpoint;

            connection.Tags.Add(TagConstants.TheirDid, connectionDetails.Did);
            await RecordService.UpdateAsync(wallet, connection);
        }

        /// <inheritdoc />
        public virtual Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.GetConnection, "ConnectionId {0}", connectionId);

            return RecordService.GetAsync<ConnectionRecord>(wallet, connectionId);
        }

        /// <inheritdoc />
        public virtual Task<List<ConnectionRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100)
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