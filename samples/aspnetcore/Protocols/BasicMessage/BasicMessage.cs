using System;
using AgentFramework.Core.Messages;
using WebAgent.Messages;
using Newtonsoft.Json;

namespace WebAgent.Protocols.BasicMessage
{
    public class BasicMessage : IAgentMessage
    {
        [JsonProperty("@id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("@type")]
        public string Type { get; set; } = CustomMessageTypes.BasicMessageType;

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("sent_time")]
        public DateTime SentTime { get; set; }
    }
}