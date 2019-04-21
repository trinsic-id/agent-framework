using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentFramework.TestHarness.Mock
{
    public class MockAgentRouter
    {
        public void RegisterAgent(MockAgent agent)
        {
            Func<(string name, byte[] data), Task<byte[]>> function = async (cb) => await agent.HandleInboundAsync(cb.data);
            _agentInBoundCallBacks.Add((agent.Name, function));
        }

        public Task<byte[]> RouteMessage(string name, byte[] data)
        {
            var result = _agentInBoundCallBacks.FirstOrDefault(_ => _.name == name);
            return result.callback.Invoke((name,data));
        }

        private readonly List<(string name, Func<(string name, byte[] data), Task<byte[]>> callback)> _agentInBoundCallBacks = new List<(string name, Func<(string name, byte[] data), Task<byte[]>> callback)>();
    }
}
