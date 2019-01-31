using System;
using System.Collections.Generic;
using System.Text;
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers.Internal
{
    internal class OutgoingMessage : IAgentMessage
    {
        [JsonProperty("@id")] public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("@type")] public string Type { get; set; } = "did:test:123;/spec/internal/outgoing";

        public string Message { get; set; }
    }
}
