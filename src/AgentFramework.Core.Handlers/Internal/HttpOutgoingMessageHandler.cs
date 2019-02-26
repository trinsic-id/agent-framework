using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class HttpOutgoingMessageHandler : MessageHandlerBase<HttpOutgoingMessage>
    {
        private readonly HttpClient _httpClient;

        internal HttpOutgoingMessageHandler(HttpMessageHandler handler)
        {
            _httpClient = new HttpClient(handler ?? new HttpClientHandler());
        }
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        protected override async Task ProcessAsync(HttpOutgoingMessage message, IAgentContext agentContext)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(agentContext.Connection.Endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(message.Message.GetUTF8Bytes())
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