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
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultMessageService : IMessageService
    {
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        protected readonly ILogger<DefaultMessageService> Logger;
        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.Core.Runtime.DefaultMessageService"/> class.
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

            var forward = await CryptoUtils.PackAsync(
                wallet, connection.Endpoint.Verkey, null,
                new ForwardMessage {Message = inner.GetUTF8String(), To = encryptionKey});

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(connection.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(forward)
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

        /// <inheritdoc />
        //public virtual async Task<(byte[], string)> RecieveAsync(AgentContext agentContext, byte[] rawMessage)
        //{
        //    try
        //    {
        //        var wireMessage = rawMessage.ToObject<AgentWireMessage>();

        //        var forwardMessage = (await Crypto.AnonDecryptAsync(
        //                agentContext.Wallet,
        //                wireMessage.To,
        //                wireMessage.Message.GetBytesFromBase64()))
        //            .ToObject<ForwardMessage>();

        //        var innerWireMessage = forwardMessage.Message.ToObject<AgentWireMessage>();

        //        var authDecrypt = await Crypto.AuthDecryptAsync(
        //            agentContext.Wallet,
        //            innerWireMessage.To,
        //            innerWireMessage.Message.GetBytesFromBase64());

        //        return (authDecrypt.MessageData, forwardMessage.To);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new AgentFrameworkException(
        //            ErrorCode.MessageUnpackError, "Failed to unpack message", e);
        //    }
        //}
    }
}