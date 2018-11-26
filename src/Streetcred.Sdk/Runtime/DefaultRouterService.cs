using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Messages.Routing;
using Streetcred.Sdk.Models.Records;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultRouterService : IRouterService
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<DefaultRouterService> _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Streetcred.Sdk.Runtime.DefaultRouterService"/> class.
        /// </summary>
        public DefaultRouterService(IMessageSerializer messageSerializer, ILogger<DefaultRouterService> logger)
        {
            _messageSerializer = messageSerializer;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connectionRecord)
        {
            _logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", connectionRecord.TheirVk, connectionRecord.Endpoint.Uri);
            
            //Create a wire message for the destination agent
            var wireMessage = await _messageSerializer.AuthPackAsync(wallet, message, connectionRecord.TheirVk, connectionRecord.MyVk);

            var innerMessage = Convert.ToBase64String(wireMessage);

            var forwardMessage = new ForwardMessage
            {
                To = connectionRecord.TheirVk,
                Message = innerMessage
            };

            //Pack this message inside another and encrypt for the agent endpoint
            var agentEndpointWireMessage = await _messageSerializer.AnonPackAsync(forwardMessage, connectionRecord.Endpoint.Verkey);
            
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(connectionRecord.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(agentEndpointWireMessage)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
