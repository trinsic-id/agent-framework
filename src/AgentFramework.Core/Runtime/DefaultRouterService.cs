using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultRouterService : IRouterService
    {
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        private readonly IWalletRecordService _walletRecordService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<DefaultRouterService> _logger;
        private readonly HttpClient _httpClient; 

        //TODO split this service into two a message delivery service and a routing service

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.Core.Runtime.DefaultRouterService"/> class.
        /// </summary>
        public DefaultRouterService(IMessageSerializer messageSerializer, ILogger<DefaultRouterService> logger, HttpClient client, IWalletRecordService walletRecordService)
        {
            _messageSerializer = messageSerializer;
            _logger = logger;
            _httpClient = client;
            _walletRecordService = walletRecordService;
        }

        /// <inheritdoc />
        public async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null)
        {
            _logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0}", connection.TheirVk);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            recipientKey = recipientKey ?? connection.TheirVk;
            byte[] wireMessage = await _messageSerializer.AuthPackAsync(wallet, message, recipientKey, connection.MyVk);
            await SendAsync(wallet, wireMessage, connection, recipientKey);
        }

        /// <inheritdoc />
        public async Task SendAsync(Wallet wallet, byte[] message, ConnectionRecord connection,
            string recipientKey = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            //TODO we prob need to load the provisioning record here so we know what services we use to send messages

            //TODO we need here is where we need a resolver
            if (connection.Services.Any(_ => _.Type == DidServiceTypes.Agency))
            {
                var agencyService = connection.Services.First(_ => _.Type == DidServiceTypes.Agency) as AgencyService;
                await ForwardToSecureEndpointAsync(agencyService.ServiceEndpoint, agencyService.Verkey, message, recipientKey);
            }
            else if (connection.Services.Any(_ => _.Type == DidServiceTypes.Agent))
            {
                var agentService = connection.Services.First(_ => _.Type == DidServiceTypes.Agent) as AgentService;
                await SendToEndpointAsync(agentService.ServiceEndpoint, message);
            }
            else
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState, "Found no recognized services on the connection record");
        }

        /// <inheritdoc />
        public async Task SendCreateMessageRoute(Wallet wallet, string recipientIdentifier, ConnectionRecord routerConnection)
        {
            var createRouteMessage = new CreateRouteMessage
            {
                RecipientIdentifier = recipientIdentifier
            };
            await SendAsync(wallet, createRouteMessage, routerConnection);
        }

        /// <inheritdoc />
        public async Task SendDeleteMessageRoute(Wallet wallet, string recipientIdentifier,
            ConnectionRecord routerConnection)
        {
            var deleteRouteMessage = new DeleteRouteMessage
            {
                RecipientIdentifier = recipientIdentifier
            };
            await SendAsync(wallet, deleteRouteMessage, routerConnection);
        }

        /// <inheritdoc />
        public async Task<RouteRecord> GetRouteRecordAsync(Wallet wallet, string id)
        {
            _logger.LogInformation(LoggingEvents.GetConnection, "Id {0}", id);

            var record = await _walletRecordService.GetAsync<RouteRecord>(wallet, id);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Route record not found");

            return record;
        }

        /// <inheritdoc />
        public async Task<IList<RouteRecord>> GetRoutesRecordsAsync(Wallet wallet, string connectionId = null)
        {
            ISearchQuery query = null;

            if (!string.IsNullOrEmpty(connectionId))
                query =  SearchQuery.Equal(nameof(RouteRecord.ConnectionId), connectionId);

            return await _walletRecordService.SearchAsync<RouteRecord>(wallet, query, null, 100);
        }

        /// <inheritdoc />
        public async Task CreateRouteRecordAsync(Wallet wallet, string recipientIdentifier, string connectionId)
        {
            if (string.IsNullOrEmpty(recipientIdentifier))
                throw new ArgumentNullException(nameof(recipientIdentifier));

            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentNullException(nameof(connectionId));

            var route = new RouteRecord
            {
                Id = recipientIdentifier,
                ConnectionId = connectionId
            };

            await _walletRecordService.AddAsync(wallet, route);
        }

        /// <inheritdoc />
        public async Task DeleteRouteRecordAsync(Wallet wallet, string recipientIdentifier)
        {
            var record = await GetRouteRecordAsync(wallet, recipientIdentifier);
            await _walletRecordService.DeleteAsync<RouteRecord>(wallet, record.Id);
        }

        /// <inheritdoc />
        public async Task ProcessForwardMessageAsync(Wallet wallet, ForwardMessage message)
        {
            var route = await _walletRecordService.GetAsync<RouteRecord>(wallet, message.To);

            var connection = await _walletRecordService.GetAsync<ConnectionRecord>(wallet, route.ConnectionId);

            var messageRawContents = Convert.FromBase64String(message.Message);

            await SendAsync(wallet, messageRawContents, connection);
        }

        /// <inheritdoc />
        public async Task ProcessCreateRouteMessageAsync(Wallet wallet, CreateRouteMessage message, ConnectionRecord connection)
        {
            await CreateRouteRecordAsync(wallet, message.RecipientIdentifier, connection.Id);
            
            //TODO should prob send back a confirmation message
        }

        /// <inheritdoc />
        public async Task ProcessDeleteRouteMessageAsync(Wallet wallet, DeleteRouteMessage message, ConnectionRecord connection)
        {
            var route = await GetRouteRecordAsync(wallet, message.RecipientIdentifier);

            if (route.ConnectionId != connection.Id)
                throw new AgentFrameworkException(ErrorCode.InvalidOperation, $"Cannot delete routing record with id : {message.RecipientIdentifier} because the route isn't owned by connection {connection.Id}");

            await DeleteRouteRecordAsync(wallet, message.RecipientIdentifier);

            //TODO should prob send back a confirmation message
        }

        /// <inheritdoc />
        public async Task<byte[]> PackForwardMessage(string verkey, byte[] message, string recipientIdentifier)
        {
            var innerMessage = Convert.ToBase64String(message);

            IAgentMessage forwardMessage = new ForwardMessage
            {
                Message = innerMessage,
                To = recipientIdentifier
            };

            return await _messageSerializer.AnonPackAsync(forwardMessage, verkey);
        }

        /// <summary>
        /// Forwards a message to a secure endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint to forward to.</param>
        /// <param name="verkey">Verkey of the endpoint to secure the message with</param>
        /// <param name="message">The raw message to forward.</param>
        /// <param name="recipientIdentifier">Identifier of the recipient to pack in the outer forward message.</param>
        /// <returns></returns>
        private async Task ForwardToSecureEndpointAsync(string endpoint, string verkey, byte[] message, string recipientIdentifier)
        {
            var wireMessage = await PackForwardMessage(verkey, message, recipientIdentifier);
            await SendToEndpointAsync(endpoint, wireMessage);
        }
        
        /// <summary>
        /// Sends a message to an endpoint.
        /// </summary>
        /// <param name="endpoint">Endpoint URI in string format.</param>
        /// <param name="message">Raw message as a byte array.</param>
        /// <returns>The response async.</returns>
        private async Task SendToEndpointAsync(string endpoint, byte[] message)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(message)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue(AgentWireMessageMimeType);

            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send A2A message", e);
            }
        }
    }
}
