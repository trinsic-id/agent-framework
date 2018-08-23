using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PairwiseApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Connections;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Runtime
{
    public class ConnectionService : IConnectionService
    {
        private readonly IWalletRecordService _recordService;
        private readonly IRouterService _routerService;
        private readonly IEndpointService _endpointService;
        private readonly IMessageSerializer _messageSerializer;

        public ConnectionService(
            IWalletRecordService recordService,
            IRouterService routerService,
            IEndpointService endpointService,
            IMessageSerializer messageSerializer)
        {
            _routerService = routerService;
            _endpointService = endpointService;
            _messageSerializer = messageSerializer;
            _recordService = recordService;
        }

        /// <inheritdoc />
        public async Task<ConnectionInvitation> CreateInvitationAsync(Wallet wallet, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId)) throw new Exception("ConnectionId must have a value.");

            var connectionKey = await Crypto.CreateKeyAsync(wallet, "{}");

            var connection = new ConnectionRecord();
            connection.ConnectionId = connectionId;
            connection.Tags.Add("connectionKey", connectionKey);

            await connection.TriggerAsync(ConnectionTrigger.InvitationCreate);
            await _recordService.AddAsync(wallet, connection);

            return new ConnectionInvitation
            {
                Endpoint = await _endpointService.GetEndpointAsync(wallet),
                ConnectionKey = connectionKey
            };
        }

        /// <inheritdoc />
        public async Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitation invitation)
        {
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

            var connectionDetails = new ConnectionDetails
            {
                Did = my.Did,
                Verkey = my.VerKey,
                Endpoint = await _endpointService.GetEndpointAsync(wallet)
            };

            var request = await _messageSerializer.PackSealedAsync<ConnectionRequest>(connectionDetails, wallet, my.VerKey,
                invitation.ConnectionKey);
            request.Key = invitation.ConnectionKey;

            var forwardMessage = new ForwardToKeyEnvelopeMessage
            {
                Key = invitation.ConnectionKey,
                Content = JsonConvert.SerializeObject(request)
            };

            await _routerService.ForwardAsync(forwardMessage, invitation.Endpoint);

            return connection.GetId();
        }

        /// <inheritdoc />
        public async Task<string> StoreRequestAsync(Wallet wallet, ConnectionRequest request)
        {
            var connectionSearch = await _recordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery { { "connectionKey", request.Key } }, null);

            var connection = connectionSearch.Single();

            var (their, theirKey) = await _messageSerializer.UnpackSealedAsync<ConnectionDetails>(request.Content, wallet, request.Key);

            if (!their.Verkey.Equals(theirKey)) throw new ArgumentException("Signed and enclosed keys don't match");

            var my = await Did.CreateAndStoreMyDidAsync(wallet, "{}");

            await Did.StoreTheirDidAsync(wallet, JsonConvert.SerializeObject(new { did = their.Did, verkey = their.Verkey }));

            connection.Endpoint = their.Endpoint;
            connection.TheirDid = their.Did;
            connection.MyDid = my.Did;
            connection.Tags.Add("myDid", my.Did);
            connection.Tags.Add("theirDid", their.Did);

            await _recordService.UpdateAsync(wallet, connection);

            return connection.GetId();
        }

        /// <summary>
        /// Accepts the connection request and sends a connection response
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns></returns>
        public async Task AcceptRequestAsync(Wallet wallet, string connectionId)
        {
            var connection = await GetAsync(wallet, connectionId);

            await connection.TriggerAsync(ConnectionTrigger.Request);

            await Pairwise.CreateAsync(wallet, connection.TheirDid, connection.MyDid, JsonConvert.SerializeObject(connection.Endpoint));
            await _recordService.UpdateAsync(wallet, connection);

            // Send back response message
            var myKey = await Did.KeyForLocalDidAsync(wallet, connection.MyDid);
            var response = new ConnectionDetails
            {
                Did = connection.MyDid,
                Verkey = myKey
            };

            var responseMessage =
                await _messageSerializer.PackSealedAsync<ConnectionResponse>(response, wallet, myKey, await Did.KeyForLocalDidAsync(wallet, connection.TheirDid));
            responseMessage.To = connection.TheirDid;


            var forwardMessage = new ForwardEnvelopeMessage
            {
                To = connection.TheirDid,
                Content = JsonConvert.SerializeObject(responseMessage)
            };

            await _routerService.ForwardAsync(forwardMessage, connection.Endpoint);
        }

        /// <inheritdoc />
        public async Task AcceptResponseAsync(Wallet wallet, ConnectionResponse response)
        {
            var connectionSearch = await _recordService.SearchAsync<ConnectionRecord>(wallet,
                new SearchRecordQuery { { "myDid", response.To } }, null);

            var connection = connectionSearch.Single();
            await connection.TriggerAsync(ConnectionTrigger.Response);

            var (connectionDetails, _) = await _messageSerializer.UnpackSealedAsync<ConnectionDetails>(response.Content, wallet,
                await Did.KeyForLocalDidAsync(wallet, response.To));

            await Did.StoreTheirDidAsync(wallet,
                JsonConvert.SerializeObject(new {did = connectionDetails.Did, verkey = connectionDetails.Verkey}));

            await Pairwise.CreateAsync(wallet, connectionDetails.Did, connection.MyDid,
                JsonConvert.SerializeObject(connectionDetails.Endpoint));

            connection.TheirDid = connectionDetails.Did;
            connection.Endpoint = connectionDetails.Endpoint;
            connection.Tags.Add("theirDid", connectionDetails.Did);
            await _recordService.UpdateAsync(wallet, connection);
        }

        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection identifier.</param>
        public Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId) => _recordService.GetAsync<ConnectionRecord>(wallet, connectionId);

        /// <summary>
        /// Lists the async.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <returns>
        /// The async.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<List<ConnectionRecord>> ListAsync(Wallet wallet) => _recordService.SearchAsync<ConnectionRecord>(wallet, null, null);
    }
}
