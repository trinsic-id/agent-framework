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

            recipientKey = recipientKey ?? connection.TheirVk;
            byte[] wireMessage = await _messageSerializer.AuthPackAsync(wallet, message, recipientKey, connection.MyVk);
            await SendAsync(wallet, wireMessage, connection, recipientKey);
        }

        /// <inheritdoc />
        public async Task SendAsync(Wallet wallet, byte[] message, ConnectionRecord connection,
            string recipientKey = null)
        {
            //TODO we need here is where we need a resolver
            if (connection.Services.Any(_ => _.Type == DidServiceTypes.Agency))
            {
                var agencyService = connection.Services.First(_ => _.Type == DidServiceTypes.Agency) as AgencyService;
                await ForwardToAgencyServiceAsync(message, agencyService, recipientKey);
            }
            else if (connection.Services.Any(_ => _.Type == DidServiceTypes.Agent))
            {
                var agencyService = connection.Services.First(_ => _.Type == DidServiceTypes.Agent) as AgentService;
                await SendToEndpointAsync(agencyService.ServiceEndpoint, message);
            }
            else
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState, "Found no recognized services on the connection record");
        }

        public async Task<RouteRecord> GetRoute(Wallet wallet, string id)
        {
            return await _walletRecordService.GetAsync<RouteRecord>(wallet, id);
        }

        /// <inheritdoc />
        public async Task<IList<RouteRecord>> GetRoutesAsync(Wallet wallet, string connectionId = null)
        {
            ISearchQuery query = null;

            if (!string.IsNullOrEmpty(connectionId))
                query =  SearchQuery.Equal(nameof(RouteRecord.ConnectionId), connectionId);

            return await _walletRecordService.SearchAsync<RouteRecord>(wallet, query, null, 100);
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
            var route = new RouteRecord
            {
                Id = message.RecipientIdentifier,
                ConnectionId = connection.Id
            };

            await _walletRecordService.AddAsync(wallet, route);
        }

        /// <inheritdoc />
        public async Task ProcessDeleteRouteMessageAsync(Wallet wallet, DeleteRouteMessage message, ConnectionRecord connection)
        {
            var route = await GetRoute(wallet, message.RecipientIdentifier);

            if (route.ConnectionId != connection.Id)
                throw new AgentFrameworkException(ErrorCode.InvalidOperation, $"Cannot delete routing record with id : {message.RecipientIdentifier} because the route isn't owned by connection {connection.Id}");

            await _walletRecordService.DeleteAsync<RouteRecord>(wallet, message.RecipientIdentifier);
        }

        /// <summary>
        /// Forwards a message to an agency service.
        /// </summary>
        /// <param name="wallet">The wallet of the sender.</param>
        /// <param name="message">The raw message to forward.</param>
        /// <param name="service">The agency service to forward the message to.</param>
        /// <param name="recipientIdentifier">The verkey of the end recipient.</param>
        /// <returns></returns>
        private async Task ForwardToAgencyServiceAsync(byte[] message, AgencyService service, string recipientIdentifier)
        {
            var innerMessage = Convert.ToBase64String(message);

            var forwardMessage = new ForwardMessage
            {
                Message = innerMessage,
                To = recipientIdentifier
            };

            //Pack this message inside another and encrypt for the agent endpoint
            var agentEndpointWireMessage = await _messageSerializer.AnonPackAsync(forwardMessage, service.Verkey);

            await SendToEndpointAsync(service.ServiceEndpoint, agentEndpointWireMessage);
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
