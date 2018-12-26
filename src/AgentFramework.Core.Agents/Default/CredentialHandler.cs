using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgentFramework.Core.Agents.Default
{
    public class CredentialHandler : IHandler
    {
        public IEnumerable<string> SupportedMessageTypes { get; }
        public Task OnMessageAsync(string agentMessage, AgentContext context)
        {
            throw new NotImplementedException();
        }
    }
}
