using System;
using AgentFramework.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using WebAgent.Messages;
using WebAgent.Protocols.BasicMessage;

namespace WebAgent
{
    public class WebAgentMiddleware : AgentMiddleware
    {
        public WebAgentMiddleware(
            RequestDelegate next,
            IServiceProvider serviceProvider)
            : base(next, serviceProvider)
        {
            AddConnectionHandler();
            AddForwardHandler();
            AddHandler<BasicMessageHandler>();
            AddHandler<TrustPingMessageHandler>();
        }
    }
}