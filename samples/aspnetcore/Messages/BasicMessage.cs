using System;
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace WebAgent.Messages
{
    public class BasicMessage : IAgentMessage
    {
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("@type")]
        public string Type { get; set; } = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/basicmessage/1.0/message";

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("sent_time")]
        public DateTime SentTime { get; set; }
    }
}