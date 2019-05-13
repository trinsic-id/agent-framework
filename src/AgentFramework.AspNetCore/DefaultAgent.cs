using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;

namespace AgentFramework.AspNetCore
{
    public class DefaultAgent : AgentMessageProcessorBase, IAgent
    {
        public DefaultAgent(IServiceProvider provider) : base(provider)
        {
        }

        public IList<IMessageHandler> Handlers => _handlers;

        async Task<MessageResponse> IAgent.ProcessAsync(IAgentContext context, MessageContext messageContext)
        {
            var response = new MessageResponse();
            response.Write(await ProcessAsync(context, messageContext));

            return response;
        }
    }
}
