using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace WebAgent.Messages
{
    public class PrivateMessage : IAgentMessage
    {
        [JsonProperty("@type")]
        public string Type { get; set; } = "did:test:123456;spec/1.0/webagent/private_message";

        [JsonProperty("Text")]
        public string Text { get; set; }
    }
}