using System;
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class HttpOutgoingMessage : IAgentMessage
    {
        [JsonProperty("@id")] public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("@type")]
        public string Type { get; set; } = "did:test:123;/spec/internal/http_outgoing";

        public string Message { get; set; }
    }
}
