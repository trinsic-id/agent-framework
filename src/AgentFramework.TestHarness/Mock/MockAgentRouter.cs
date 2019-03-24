using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentFramework.TestHarness.Mock
{
    public class MockAgentRouter
    {
        public void RegisterAgent(MockAgent agent)
        {
            _agentInBoundCallBacks.Add((agent.Name, data => agent.HandleInboundAsync(data)));
        }

        public void RouteMessage(string name, byte[] data)
        {
            var result = _agentInBoundCallBacks.FirstOrDefault(_ => _.name == name);
            result.callback.Invoke(data);
        }

        private readonly List<(string name, Action<byte[]> callback)> _agentInBoundCallBacks = new List<(string name, Action<byte[]> callback)>();
    }
}
