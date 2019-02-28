using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators;
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
    public class DefaultMessageService : IMessageService
    {
        /// <summary>The agent wire message MIME type</summary>
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        /// <summary>
        /// The outgoing message decorators.
        /// </summary>
        protected readonly IEnumerable<IOutgoingMessageDecoratorHandler> OutgoingMessageDecorators;

        /// <summary>The logger</summary>
        // ReSharper disable InconsistentNaming
        protected readonly ILogger<DefaultMessageService> Logger;

        /// <summary>The HTTP client</summary>
        protected readonly HttpClient HttpClient;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessageService"/> class.
        /// </summary>
        public DefaultMessageService(IEnumerable<IOutgoingMessageDecoratorHandler> outgoingMessageDecorators, ILogger<DefaultMessageService> logger, HttpClient httpClient)
        {
            OutgoingMessageDecorators = outgoingMessageDecorators;
            Logger = logger;
            HttpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<byte[]> PrepareAsync(Wallet wallet, OutgoingMessageContext messageContext, string recipientKey, string[] routingKeys = null, string senderKey = null)
        {
            foreach (var outgoingMessageDecoratorHandler in OutgoingMessageDecorators)
            {
                messageContext = await outgoingMessageDecoratorHandler.ProcessAsync(messageContext, wallet);
            }

            var msg = await CryptoUtils.PackAsync(wallet, recipientKey, senderKey, messageContext.OutboundMessage.ToByteArray());

            var previousKey = recipientKey;

            if (routingKeys != null)
            {
                foreach (var routingKey in routingKeys)
                {
                    msg = await CryptoUtils.PackAsync(
                        wallet, routingKey, null,
                        new ForwardMessage {Message = msg.GetUTF8String(), To = previousKey});
                    previousKey = routingKey;
                }
            }

            return msg;
        }

        /// <inheritdoc />
        public virtual async Task SendToConnectionAsync(Wallet wallet, OutgoingMessageContext messageContext, ConnectionRecord connection, string recipientKey = null)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", connection.TheirVk,
                connection.Endpoint.Uri);

            var message = messageContext.OutboundMessage;

            if (string.IsNullOrEmpty(message.Id))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@id field on message must be populated");

            if (string.IsNullOrEmpty(message.Type))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@type field on message must be populated");

            recipientKey = recipientKey
                                ?? connection.TheirVk
                                ?? throw new AgentFrameworkException(
                                    ErrorCode.A2AMessageTransmissionError, "Cannot find encryption key");

            var routingKeys = connection.Endpoint?.Verkey != null ? new[] {connection.Endpoint.Verkey} : new string[0];

            var wireMsg = await PrepareAsync(wallet, messageContext, recipientKey, routingKeys, connection.MyVk);
            
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(connection.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(wireMsg)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(AgentWireMessageMimeType);

            try
            {
                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                throw new AgentFrameworkException(
                    ErrorCode.A2AMessageTransmissionError, "Failed to send A2A message", e);
            }
        }
    }
}