using System;
using System.Collections.Generic;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Handlers.Default;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebAgent.Messages
{
    public class WebAgentMiddleware : AgentMiddleware
    {
        public WebAgentMiddleware(
            RequestDelegate next,
            IWalletService walletService,
            IPoolService poolService,
            IServiceProvider serviceProvider,
            IOptions<WalletOptions> walletOptions,
            IOptions<PoolOptions> poolOptions)
            : base(next, walletService, poolService, serviceProvider, walletOptions, poolOptions)
        {
        }

        public override IEnumerable<IMessageHandler> Handlers => new IMessageHandler[]
        {
            ServiceProvider.GetService<ConnectionHandler>(),
            ServiceProvider.GetService<PrivateMessageHandler>()
        };
    }
}