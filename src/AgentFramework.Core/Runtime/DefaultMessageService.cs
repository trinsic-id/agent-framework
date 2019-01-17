using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Messaging;
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
        public virtual async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connectionRecord,
            string recipientKey = null)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", connectionRecord.TheirVk,
                connectionRecord.Endpoint.Uri);

            var encryptionKey = recipientKey
                                ?? connectionRecord.TheirVk
                                ?? throw new AgentFrameworkException(
                                    ErrorCode.A2AMessageTransmissionError, "Cannot find encryption key");

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
                    .ToJson(),
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
        public virtual async Task<(byte[], string)> RecieveAsync(AgentContext agentContext, byte[] rawMessage)
        {
            try
            {
                var wireMessage = rawMessage.ToObject<AgentWireMessage>();

                var forwardMessage = (await Crypto.AnonDecryptAsync(
                        agentContext.Wallet,
                        wireMessage.To,
                        wireMessage.Message.GetBytesFromBase64()))
                    .ToObject<ForwardMessage>();

                var innerWireMessage = forwardMessage.Message.ToObject<AgentWireMessage>();

                var authDecrypt = await Crypto.AuthDecryptAsync(
                    agentContext.Wallet,
                    innerWireMessage.To,
                    innerWireMessage.Message.GetBytesFromBase64());

                return (authDecrypt.MessageData, forwardMessage.To);
            }
            catch (Exception e)
            {
                throw new AgentFrameworkException(
                    ErrorCode.MessageUnpackError, "Failed to unpack message", e);
            }
        }
    }
}