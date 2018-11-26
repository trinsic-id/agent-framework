using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Streetcred.Sdk.Messages
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
