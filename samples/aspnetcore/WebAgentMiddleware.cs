using System;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebAgent.Messages;
using WebAgent.Protocols;
using WebAgent.Protocols.BasicMessage;

namespace WebAgent
{
    public class WebAgentMiddleware : AgentMiddleware
    {
        public WebAgentMiddleware(
            RequestDelegate next,
            IWalletService walletService,
            IServiceProvider serviceProvider,
            IOptions<WalletOptions> walletOptions)
            : base(next, walletService, serviceProvider, walletOptions)
        {
            AddHandler<BasicMessageHandler>();
            AddHandler<TrustPingMessageHandler>();
        }
    }
}