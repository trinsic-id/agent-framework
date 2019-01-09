using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace AgentFramework.Core.Handlers
{
    public abstract class MessageHandlerBase<T> : IMessageHandler
        where T : IAgentMessage, new()
    {
        private readonly string _supportedMessageType;

        protected MessageHandlerBase()
        {
            _supportedMessageType = new T().Type;
        }

        public IEnumerable<string> SupportedMessageTypes => new[] {_supportedMessageType};

        protected abstract Task HandleAsync(T message);

        public Task OnMessageAsync(string agentMessage, AgentContext context) =>
            HandleAsync(JsonConvert.DeserializeObject<T>(agentMessage));
    }
}