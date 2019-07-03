using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents a private message record in the user's wallet
    /// </summary>
    /// <seealso cref="AgentFramework.Core.Models.Records.RecordBase" />
    public class BasicMessageRecord : RecordBase
    {
        public BasicMessageRecord()
        {
            Id = Guid.NewGuid().ToString();
        }

        public override string TypeName => "WebAgent.BasicMessage";

        [JsonIgnore]
        public string ConnectionId
        {
            get => Get();
            set => Set(value);
        }

        public DateTime SentTime { get; set; }

        public MessageDirection Direction { get; set; }

        public string Text { get; set; }
    }

    public enum MessageDirection
    {
        Incoming,
        Outgoing
    }
}
