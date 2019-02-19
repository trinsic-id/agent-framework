using System;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
            IConnectionService connectionService,
            IServiceProvider serviceProvider,
            IOptions<WalletOptions> walletOptions,
            ILogger<AgentBase> logger)
            : base(next, walletService, serviceProvider, walletOptions, connectionService, logger)
        {
            AddConnectionHandler();
            AddForwardHandler();
            AddHandler<BasicMessageHandler>();
            AddHandler<TrustPingMessageHandler>();
        }
    }
}