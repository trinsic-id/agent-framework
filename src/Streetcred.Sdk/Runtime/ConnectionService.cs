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
        public async Task<ConnectionInvitation> CreateInvitationAsync(Wallet wallet, string connectionId)
        {
            _logger.LogInformation(LoggingEvents.CreateInvitation, "ConnectionId {0}", connectionId);

            var connectionKey = await Crypto.CreateKeyAsync(wallet, "{}");

            var connection = new ConnectionRecord();
            connection.ConnectionId = connectionId;
            connection.Tags.Add("connectionKey", connectionKey);

            await connection.TriggerAsync(ConnectionTrigger.InvitationCreate);
            await _recordService.AddAsync(wallet, connection);

            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

            var invite = new ConnectionInvitation
            {
                Endpoint = provisioning.Endpoint,
                ConnectionKey = connectionKey
            };

            if (provisioning.Owner == null) return invite;

            invite.Name = provisioning.Owner.Name;
            invite.ImageUrl = provisioning.Owner.ImageUrl;

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
                ConnectionId = Guid.NewGuid().ToString().ToLowerInvariant()
            };
            connection.Tags.Add("myDid", my.Did);

            await connection.TriggerAsync(ConnectionTrigger.InvitationAccept);
            await _recordService.AddAsync(wallet, connection);

            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);
            var connectionDetails = new ConnectionDetails
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = provisioning.Endpoint
            };

            var request = await _messageSerializer.PackSealedAsync<ConnectionRequest>(connectionDetails, wallet, my.VerKey,
                invitation.ConnectionKey);
            request.Key = invitation.ConnectionKey;

            var forwardMessage = new ForwardToKeyEnvelopeMessage
            {
                Key = invitation.ConnectionKey,
                Content = request.ToJson()
            };

            await _routerService.ForwardAsync(forwardMessage, invitation.Endpoint);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task<string> StoreRequestAsync(Wallet wallet, ConnectionRequest request)
        {
            _logger.LogInformation(LoggingEvents.StoreConnectionRequest, "Key {0}", request.Key);

            var connectionSearch = await _recordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery { { "connectionKey", request.Key } }, null);

            var connection = connectionSearch.Single();

            var (their, theirKey) = await _messageSerializer.UnpackSealedAsync<ConnectionDetails>(request.Content, wallet, request.Key);

            if (!their.Verkey.Equals(theirKey)) throw new ArgumentException("Signed and enclosed keys don't match");

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            await Did.StoreTheirDidAsync(wallet, new { did = their.Did, verkey = their.Verkey }.ToJson());

            connection.Endpoint = their.Endpoint;
            connection.TheirDid = their.Did;
            connection.MyDid = my.Did;
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
            var myKey = await Did.KeyForLocalDidAsync(wallet, connection.MyDid);
            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);
            var response = new ConnectionDetails
            {
                Did = connection.MyDid,
                Endpoint = provisioning.Endpoint,
                Verkey = myKey
            };

            var responseMessage =
                await _messageSerializer.PackSealedAsync<ConnectionResponse>(response, wallet, myKey, await Did.KeyForLocalDidAsync(wallet, connection.TheirDid));
            responseMessage.To = connection.TheirDid;


            var forwardMessage = new ForwardEnvelopeMessage
            {
                To = connection.TheirDid,
                Content = responseMessage.ToJson()
            };

            await _routerService.ForwardAsync(forwardMessage, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task AcceptResponseAsync(Wallet wallet, ConnectionResponse response)
        {
            _logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {0}", response.To);

            var connectionSearch = await _recordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery { { "myDid", response.To } }, null);

            var connection = connectionSearch.Single();
            await connection.TriggerAsync(ConnectionTrigger.Response);

            var (connectionDetails, _) = await _messageSerializer.UnpackSealedAsync<ConnectionDetails>(response.Content, wallet,
                await Did.KeyForLocalDidAsync(wallet, response.To));

            await Did.StoreTheirDidAsync(wallet,
                new { did = connectionDetails.Did, verkey = connectionDetails.Verkey }.ToJson());

            await Pairwise.CreateAsync(wallet, connectionDetails.Did, connection.MyDid, connectionDetails.Endpoint.ToJson());
            
            connection.TheirDid = connectionDetails.Did;

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
        public Task<List<ConnectionRecord>> ListAsync(Wallet wallet)
        {
            _logger.LogInformation(LoggingEvents.ListConnections, "List Connections");

            return _recordService.SearchAsync<ConnectionRecord>(wallet, null, null);
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
