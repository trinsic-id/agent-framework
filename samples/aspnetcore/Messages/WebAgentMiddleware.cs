using System;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace WebAgent.Messages
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
        }
    }
}