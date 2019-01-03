using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Runtime
{
    public class DefaultMessagingService : IMessagingService
    {
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        protected readonly IMessageSerializer MessageSerializer;
        protected readonly ILogger<DefaultMessagingService> Logger;
        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.Core.Runtime.DefaultMessagingService"/> class.
        /// </summary>
        public DefaultMessagingService(IMessageSerializer messageSerializer, ILogger<DefaultMessagingService> logger, HttpClient client)
        {
            MessageSerializer = messageSerializer;
            Logger = logger;
            HttpClient = client;
        }

        /// <inheritdoc />
        public virtual async Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0}", connection.TheirVk);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (connection.State != ConnectionState.Connected)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");

            recipientKey = recipientKey ?? connection.TheirVk;
            byte[] wireMessage = await MessageSerializer.AuthPackAsync(wallet, message, recipientKey, connection.MyVk);
            await SendAsync(wallet, wireMessage, connection, recipientKey);
        }

        /// <inheritdoc />
        public virtual async Task SendAsync(Wallet wallet, byte[] message, ConnectionRecord connection,
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
        public virtual async Task<byte[]> PackForwardMessage(string verkey, byte[] message, string recipientIdentifier)
        {
            var innerMessage = Convert.ToBase64String(message);

            IAgentMessage forwardMessage = new ForwardMessage
            {
                Message = innerMessage,
                To = recipientIdentifier
            };

            return await MessageSerializer.AnonPackAsync(forwardMessage, verkey);
        }

        /// <summary>
        /// Forwards a message to a secure endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint to forward to.</param>
        /// <param name="verkey">Verkey of the endpoint to secure the message with</param>
        /// <param name="message">The raw message to forward.</param>
        /// <param name="recipientIdentifier">Identifier of the recipient to pack in the outer forward message.</param>
        /// <returns></returns>
        protected async Task ForwardToSecureEndpointAsync(string endpoint, string verkey, byte[] message, string recipientIdentifier)
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
        protected async Task SendToEndpointAsync(string endpoint, byte[] message)
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
                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, "Failed to send A2A message", e);
            }
        }
    }
}
