using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class HttpOutgoingMessageHandler : MessageHandlerBase<HttpOutgoingMessage>
    {
        private readonly HttpClient _httpClient;

        internal HttpOutgoingMessageHandler(HttpClientHandler handler)
        {
            _httpClient = new HttpClient(handler);
        }
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        protected override async Task ProcessAsync(HttpOutgoingMessage message, IAgentContext agentContext)
        {
            var inner = await CryptoUtils.PackAsync(
                agentContext.Wallet, agentContext.Connection.TheirVk, agentContext.Connection.MyVk, message.Message.ToByteArray());

            var forward = await CryptoUtils.PackAsync(
                agentContext.Wallet, agentContext.Connection.Endpoint.Verkey, null,
                new ForwardMessage {Message = inner.GetUTF8String(), To = agentContext.Connection.TheirVk});

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(agentContext.Connection.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(forward)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(AgentWireMessageMimeType);

            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                // Log message, store or queue for retry
            }
        }
    }
}