using System;
using System.Collections.Generic;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.Core.Handlers;

namespace AgentFramework.AspNetCore
{
    public class AgentFactory : IAgentFactory
    {
        public IServiceProvider ServiceProvider { get; }

        public AgentFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IDictionary<object, object> Properties { get; set; } = new Dictionary<object, object>();

        public T Create<T>(object param) where T : IAgent
        {
            return (T)ServiceProvider.GetService(typeof(T));
        }
    }
}
