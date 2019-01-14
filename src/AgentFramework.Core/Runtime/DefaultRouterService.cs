using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultRouterService : IRouterService
    {
        private readonly ILogger<DefaultRouterService> _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.Core.Runtime.DefaultRouterService"/> class.
        /// </summary>
        public DefaultRouterService(ILogger<DefaultRouterService> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connectionRecord,
            string recipientKey = null)
        {
            _logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", connectionRecord.TheirVk,
                connectionRecord.Endpoint.Uri);

            var encryptionKey = recipientKey ?? connectionRecord.TheirVk;

            //Create a wire message for the destination agent
            var forwardMessage = new ForwardMessage
            {
                Message = new AgentWireMessage
                    {
                        To = encryptionKey,
                        From = connectionRecord.MyVk,
                        Message = (await Crypto.AuthCryptAsync(
                                wallet,
                                connectionRecord.MyVk,
                                encryptionKey,
                                message.ToByteArray()))
                            .ToBase64String()
                    }
                    .ToByteArray()
                    .ToBase64String(),
                To = encryptionKey
            };

            //Pack this message inside another and encrypt for the agent endpoint
            var agentEndpointWireMessage = new AgentWireMessage
            {
                To = connectionRecord.Endpoint.Verkey,
                Message = (await Crypto.AnonCryptAsync(
                        connectionRecord.Endpoint.Verkey,
                        forwardMessage.ToByteArray()))
                    .ToBase64String()
            };

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(connectionRecord.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(agentEndpointWireMessage.ToByteArray())
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
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send A2A message",
                    e);
            }
        }
    }
}