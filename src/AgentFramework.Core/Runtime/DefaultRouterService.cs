using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultRouterService : IRouterService
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<DefaultRouterService> _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.Core.Runtime.DefaultRouterService"/> class.
        /// </summary>
        public DefaultRouterService(IMessageSerializer messageSerializer, ILogger<DefaultRouterService> logger)
        {
            _messageSerializer = messageSerializer;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connectionRecord, string recipientKey = null)
        {
            _logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", connectionRecord.TheirVk, connectionRecord.Endpoint.Uri);

            byte[] wireMessage;

            //Create a wire message for the destination agent
            if (string.IsNullOrEmpty(recipientKey))
                wireMessage = await _messageSerializer.AuthPackAsync(wallet, message, connectionRecord.TheirVk, connectionRecord.MyVk);
            else
                wireMessage = await _messageSerializer.AuthPackAsync(wallet, message, recipientKey, connectionRecord.MyVk);

            var innerMessage = Convert.ToBase64String(wireMessage);

            var forwardMessage = new ForwardMessage { Message = innerMessage };

            if (string.IsNullOrEmpty(recipientKey))
                forwardMessage.To = connectionRecord.TheirVk;
            else
                forwardMessage.To = recipientKey;

            //Pack this message inside another and encrypt for the agent endpoint
            var agentEndpointWireMessage = await _messageSerializer.AnonPackAsync(forwardMessage, connectionRecord.Endpoint.Verkey);
            
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(connectionRecord.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(agentEndpointWireMessage)
            };

            //TODO this mime type should be changed in accordance with the message format hipe emerging
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

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
