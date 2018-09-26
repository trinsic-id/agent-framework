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
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class ConnectionService : IConnectionService
    {
        private readonly IWalletRecordService _recordService;
        private readonly IRouterService _routerService;
        private readonly IProvisioningService _provisioningService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<ConnectionService> _logger;

        public ConnectionService(
            IWalletRecordService recordService,
            IRouterService routerService,
            IProvisioningService provisioningService,
            IMessageSerializer messageSerializer,
            ILogger<ConnectionService> logger)
        {
            _routerService = routerService;
            _provisioningService = provisioningService;
            _messageSerializer = messageSerializer;
            _logger = logger;
            _recordService = recordService;
        }

        /// <inheritdoc />
        public async Task<ConnectionInvitation> CreateInvitationAsync(Wallet wallet,
            CreateInviteConfiguration config = null)
        {
            var connectionId = !string.IsNullOrEmpty(config?.ConnectionId)
                ? config.ConnectionId
                : Guid.NewGuid().ToString();

            _logger.LogInformation(LoggingEvents.CreateInvitation, "ConnectionId {0}", connectionId);

            var connectionKey = await Crypto.CreateKeyAsync(wallet, "{}");

            var connection = new ConnectionRecord();
            connection.ConnectionId = connectionId;
            connection.Tags.Add("connectionKey", connectionKey);

            if (config?.TheirAlias != null)
            {
                connection.Alias = config.TheirAlias;
                if (!string.IsNullOrEmpty(config.TheirAlias.Name))
                    connection.Tags.Add("alias", config.TheirAlias.Name);
            }

            await _recordService.AddAsync(wallet, connection);

            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

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
        public async Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitation invitation)
        {
            _logger.LogInformation(LoggingEvents.AcceptInvitation, "Key {0}, Endpoint {1}",
                invitation.ConnectionKey, invitation.Endpoint.Uri);

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            var connection = new ConnectionRecord
            {
                Endpoint = invitation.Endpoint,
                MyDid = my.Did,
                MyVk = my.VerKey,
                ConnectionId = Guid.NewGuid().ToString().ToLowerInvariant()
            };
            connection.Tags.Add("myDid", my.Did);

            if (!string.IsNullOrEmpty(invitation.Name) || !string.IsNullOrEmpty(invitation.ImageUrl))
            {
                connection.Alias = new ConnectionAlias
                {
                    Name = invitation.Name,
                    ImageUrl = invitation.ImageUrl
                };

                if (string.IsNullOrEmpty(invitation.Name))
                    connection.Tags.Add("aliasName", invitation.Name);
            }

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await _recordService.AddAsync(wallet, connection);

            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);
            var connectionDetails = new ConnectionDetails
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = provisioning.Endpoint
            };

            var request = await _messageSerializer.PackSealedAsync<ConnectionRequest>(connectionDetails, wallet,
                my.VerKey, invitation.ConnectionKey);
            request.Key = invitation.ConnectionKey;
            request.Type = MessageUtils.FormatKeyMessageType(invitation.ConnectionKey, MessageTypes.ConnectionRequest);

            var forwardMessage = new ForwardToKeyEnvelopeMessage
            {
                Type = MessageUtils.FormatKeyMessageType(invitation.ConnectionKey, MessageTypes.ForwardToKey),
                Content = request.ToJson()
            };

            await _routerService.ForwardAsync(forwardMessage, invitation.Endpoint);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task<string> StoreRequestAsync(Wallet wallet, ConnectionRequest request)
        {
            _logger.LogInformation(LoggingEvents.StoreConnectionRequest, "Key {0}", request.Key);

            var (didOrKey, _) = MessageUtils.ParseMessageType(request.Type);

            var connectionSearch = await _recordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery {{"connectionKey", didOrKey}}, null, 1);

            var connection = connectionSearch.Single();

            var (their, theirKey) =
                await _messageSerializer.UnpackSealedAsync<ConnectionDetails>(request.Content, wallet, request.Key);

            if (!their.Verkey.Equals(theirKey)) throw new ArgumentException("Signed and enclosed keys don't match");

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            await Did.StoreTheirDidAsync(wallet, new {did = their.Did, verkey = their.Verkey}.ToJson());

            connection.Endpoint = their.Endpoint;
            connection.TheirDid = their.Did;
            connection.TheirVk = their.Verkey;
            connection.MyDid = my.Did;
            connection.MyVk = my.VerKey;
            connection.Tags["myDid"] = my.Did;
            connection.Tags["theirDid"] = their.Did;

            await _recordService.UpdateAsync(wallet, connection);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task AcceptRequestAsync(Wallet wallet, string connectionId)
        {
            _logger.LogInformation(LoggingEvents.AcceptConnectionRequest, "ConnectionId {0}", connectionId);

            var connection = await GetAsync(wallet, connectionId);

            await connection.TriggerAsync(ConnectionTrigger.Request);

            await Pairwise.CreateAsync(wallet, connection.TheirDid, connection.MyDid, connection.Endpoint.ToJson());
            await _recordService.UpdateAsync(wallet, connection);

            // Send back response message
            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);
            var response = new ConnectionDetails
            {
                Did = connection.MyDid,
                Endpoint = provisioning.Endpoint,
                Verkey = connection.MyVk
            };

            var responseMessage =
                await _messageSerializer.PackSealedAsync<ConnectionResponse>(response, wallet, connection.MyVk,
                    connection.TheirVk);
            responseMessage.Type =
                MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.ConnectionResponse);
            responseMessage.To = connection.TheirDid;

            var forwardMessage = new ForwardEnvelopeMessage
            {
                Type = MessageUtils.FormatDidMessageType(connection.TheirDid, MessageTypes.Forward),
                Content = responseMessage.ToJson()
            };

            await _routerService.ForwardAsync(forwardMessage, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task AcceptResponseAsync(Wallet wallet, ConnectionResponse response)
        {
            _logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {0}", response.To);

            var (didOrKey, _) = MessageUtils.ParseMessageType(response.Type);

            var connectionSearch = await _recordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery {{"myDid", didOrKey}}, null, 1);

            var connection = connectionSearch.Single();
            await connection.TriggerAsync(ConnectionTrigger.Response);

            var (connectionDetails, _) = await _messageSerializer.UnpackSealedAsync<ConnectionDetails>(response.Content,
                wallet, connection.MyVk);

            await Did.StoreTheirDidAsync(wallet,
                new {did = connectionDetails.Did, verkey = connectionDetails.Verkey}.ToJson());

            await Pairwise.CreateAsync(wallet, connectionDetails.Did, connection.MyDid,
                connectionDetails.Endpoint.ToJson());

            connection.TheirDid = connectionDetails.Did;
            connection.TheirVk = connectionDetails.Verkey;

            if (connectionDetails.Endpoint != null)
                connection.Endpoint = connectionDetails.Endpoint;

            connection.Tags.Add("theirDid", connectionDetails.Did);
            await _recordService.UpdateAsync(wallet, connection);
        }

        /// <inheritdoc />
        public Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId)
        {
            _logger.LogInformation(LoggingEvents.GetConnection, "ConnectionId {0}", connectionId);

            return _recordService.GetAsync<ConnectionRecord>(wallet, connectionId);
        }

        /// <inheritdoc />
        public Task<List<ConnectionRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100)
        {
            _logger.LogInformation(LoggingEvents.ListConnections, "List Connections");

            return _recordService.SearchAsync<ConnectionRecord>(wallet, query, null, count);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Wallet wallet, string connectionId)
        {
            _logger.LogInformation(LoggingEvents.DeleteConnection, "ConnectionId {0}", connectionId);

            if ((await _recordService.GetAsync<ConnectionRecord>(wallet, connectionId)) == null)
                return true;

            return await _recordService.DeleteAsync<ConnectionRecord>(wallet, connectionId);
        }
    }
}