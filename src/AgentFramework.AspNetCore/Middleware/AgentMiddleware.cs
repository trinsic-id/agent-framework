using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Agents;
using AgentFramework.Core.Agents.Default;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Messages.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentFramework.AspNetCore.Middleware
{
    public class AgentMiddleware : AgentBase
    {
        private readonly RequestDelegate _next;
        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;
        private readonly PoolOptions _poolOptions;
        private readonly WalletOptions _walletOptions;

        public AgentMiddleware(RequestDelegate next,
            IWalletService walletService,
            IPoolService poolService,
            IServiceProvider serviceProvider,
            IOptions<WalletOptions> walletOptions,
            IOptions<PoolOptions> poolOptions)
            : base(serviceProvider)
        {
            _next = next;
            _walletService = walletService;
            _poolService = poolService;
            _poolOptions = poolOptions.Value;
            _walletOptions = walletOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (context.Request.ContentLength == null) throw new Exception("Empty content length");

            var body = new byte[(int) context.Request.ContentLength];
            await context.Request.Body.ReadAsync(body, 0, body.Length);

            await ProcessAsync(
                body,
                await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials));
                //await _poolService.GetPoolAsync(_poolOptions.PoolName, _poolOptions.ProtocolVersion));

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(string.Empty);
        }

        public override IEnumerable<IHandler> Handlers => new IHandler[]
        {
            ServiceProvider.GetService<ConnectionHandler>(),
            ServiceProvider.GetService<CredentialHandler>(),
            ServiceProvider.GetService<ProofHandler>()
        };
    }
}