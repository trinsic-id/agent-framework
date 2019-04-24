using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using AgentFramework.Core.Decorators.Transport;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultMessageService : IMessageService
    {
        /// <summary>The agent wire message MIME type</summary>
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";
        
        /// <summary>The logger</summary>
        // ReSharper disable InconsistentNaming
        protected readonly ILogger<DefaultMessageService> Logger;

        /// <summary>The HTTP client</summary>
        protected readonly HttpClient HttpClient;
        // ReSharper restore InconsistentNaming

        /// <summary>Initializes a new instance of the <see cref="DefaultMessageService"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="httpMessageHandler">The HTTP message handler.</param>
        public DefaultMessageService(
            ILogger<DefaultMessageService> logger, 
            HttpMessageHandler httpMessageHandler)
        {
            Logger = logger;
            HttpClient = new HttpClient(httpMessageHandler);
        }

        /// <inheritdoc />
        public virtual Task<byte[]> PrepareForConnectionAsync(Wallet wallet, AgentMessage message, ConnectionRecord connection, string recipientKey = null, bool routing = true)
        {
            recipientKey = recipientKey
                                ?? connection.TheirVk
                                ?? throw new AgentFrameworkException(
                                    ErrorCode.A2AMessageTransmissionError, "Cannot find encryption key");

            var routingKeys = routing && connection.Endpoint?.Verkey != null ? new[] { connection.Endpoint.Verkey } : new string[0];

            return PrepareAsync(wallet, message, recipientKey, routingKeys, connection.MyVk);
        }

        /// <inheritdoc />
        public virtual async Task<byte[]> PrepareAsync(Wallet wallet, AgentMessage message, string recipientKey, string[] routingKeys = null, string senderKey = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (recipientKey == null) throw new ArgumentNullException(nameof(recipientKey));

            // Pack application level message
            var msg = await CryptoUtils.PackAsync(wallet, recipientKey, message.ToByteArray(), senderKey);

            var previousKey = recipientKey;

            if (routingKeys != null)
            {
                // TODO: In case of multiple key, should they each wrap a forward message
                // or pass all keys to the PackAsync function as array?
                foreach (var routingKey in routingKeys)
                {
                    // Anonpack
                    msg = await CryptoUtils.PackAsync(wallet, routingKey, new ForwardMessage { Message = msg.GetUTF8String(), To = previousKey });
                    previousKey = routingKey;
                }
            }

            return msg;
        }

        /// <inheritdoc />
        public virtual async Task<MessageContext> SendToConnectionAsync(Wallet wallet, AgentMessage message, ConnectionRecord connection, string recipientKey = null, bool requestResponse = false)
        {
            recipientKey = recipientKey
                                ?? connection.TheirVk
                                ?? throw new AgentFrameworkException(
                                    ErrorCode.A2AMessageTransmissionError, "Cannot find encryption key");

            var routingKeys = connection.Endpoint?.Verkey != null ? new[] {connection.Endpoint.Verkey} : new string[0];

            var response = await SendToEndpoint(wallet, message, recipientKey, connection.Endpoint?.Uri, routingKeys, connection.MyVk, requestResponse);

            if (response?.Packed != null)
            {
                response = await UnpackWithConnectionAsync(wallet, response, connection);
            }

            return response;
        }

        private async Task<MessageContext> UnpackWithConnectionAsync(Wallet wallet, MessageContext message, ConnectionRecord connection)
        {
            UnpackResult unpacked;

            try
            {
                unpacked = await CryptoUtils.UnpackAsync(wallet, message.Payload);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to un-pack message", e);
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Failed to un-pack message", e);
            }

            message = new MessageContext(unpacked.Message, false, connection);

            return message;
        }

        public async Task<MessageContext> UnpackAsync(Wallet wallet, MessageContext message)
        {
            UnpackResult unpacked;

            try
            {
                unpacked = await CryptoUtils.UnpackAsync(wallet, message.Payload);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to un-pack message", e);
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Failed to un-pack message", e);
            }
            
            message = new MessageContext(unpacked.Message, false, message.Connection);

            return message;
        }

        /// <inheritdoc />
        public virtual async Task<MessageContext> SendToEndpoint(Wallet wallet, AgentMessage message, string recipientKey,
            string endpointUri, string[] routingKeys = null, string senderKey = null, bool requestResponse = false)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", recipientKey,
                endpointUri);

            if (string.IsNullOrEmpty(message.Id))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@id field on message must be populated");

            if (string.IsNullOrEmpty(message.Type))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@type field on message must be populated");

            if (string.IsNullOrEmpty(endpointUri))
                throw new ArgumentNullException(nameof(endpointUri));

            if (requestResponse)
                message.AddReturnRouting();

            var wireMsg = await PrepareAsync(wallet, message, recipientKey, routingKeys, senderKey);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpointUri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(wireMsg)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue(AgentWireMessageMimeType);

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new AgentFrameworkException(
                    ErrorCode.A2AMessageTransmissionError, $"Failed to send A2A message with an HTTP status code of {response.StatusCode} and content {responseBody}");
            }

            //TODO this assumes all messages are packed
            if (response.Content != null)
                return new MessageContext(await response.Content.ReadAsByteArrayAsync(), true);

            return null;
        }
    }
}