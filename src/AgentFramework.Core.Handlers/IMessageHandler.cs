using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentFramework.Core.Handlers
{
    public interface IMessageHandler
    {
        IEnumerable<string> SupportedMessageTypes { get; }

        Task OnMessageAsync(string agentMessage, AgentContext context);
    }
}