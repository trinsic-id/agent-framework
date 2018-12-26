using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentFramework.Core.Agents
{
    public interface IHandler
    {
        IEnumerable<string> SupportedMessageTypes { get; }

        Task OnMessageAsync(string agentMessage, AgentContext context);
    }
}