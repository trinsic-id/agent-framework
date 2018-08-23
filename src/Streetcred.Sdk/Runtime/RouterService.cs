using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Contracts;

namespace Streetcred.Sdk.Runtime
{
    /// <summary>
    /// Router service.
    /// </summary>
    public class RouterService : IRouterService
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Streetcred.Sdk.Runtime.RouterService"/> class.
        /// </summary>
        public RouterService(IMessageSerializer messageSerializer)
        {
            _messageSerializer = messageSerializer;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Forwards the asynchronous.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public async Task ForwardAsync(IEnvelopeMessage envelope, AgentEndpoint endpoint)
        {

            var encrypted = await _messageSerializer.PackAsync(envelope, endpoint.Verkey);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(encrypted)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
