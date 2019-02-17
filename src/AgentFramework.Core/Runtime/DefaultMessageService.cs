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

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessageService"/> class.
        /// </summary>
        public DefaultMessageService(ILogger<DefaultMessageService> logger, HttpClient httpClient)
        {
            Logger = logger;
            HttpClient = httpClient;
        }

        /// <inheritdoc />
        public virtual async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection,
            string recipientKey = null)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", connection.TheirVk,
                connection.Endpoint.Uri);

            if (string.IsNullOrEmpty(message.Id))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@id field on message must be populated");

            if (string.IsNullOrEmpty(message.Type))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@type field on message must be populated");

            var encryptionKey = recipientKey
                                ?? connection.TheirVk
                                ?? throw new AgentFrameworkException(
                                    ErrorCode.A2AMessageTransmissionError, "Cannot find encryption key");

            var inner = await CryptoUtils.PackAsync(
                wallet, encryptionKey, connection.MyVk, message.ToByteArray());

            //TODO we will have multiple forwards here in future
            byte[] forward = null;
            if (connection.Endpoint.Verkey != null)
            {
                forward = await CryptoUtils.PackAsync(
                    wallet, connection.Endpoint.Verkey, null,
                    new ForwardMessage {Message = inner.GetUTF8String(), To = encryptionKey});
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(connection.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(forward ?? inner)
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