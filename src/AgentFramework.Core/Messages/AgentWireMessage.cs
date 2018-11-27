using Newtonsoft.Json;

namespace AgentFramework.Core.Messages
{
    public class AgentWireMessage
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
