using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentFramework.AspNetCore.Middleware
{
    public class AgentMiddleware : AgentBase
    {
        private readonly RequestDelegate _next;
        private readonly IWalletService _walletService;
        private readonly WalletOptions _walletOptions;

        public AgentMiddleware(RequestDelegate next,
            IWalletService walletService,
            IServiceProvider serviceProvider,
            IOptions<WalletOptions> walletOptions)
            : base(serviceProvider)
        {
            _next = next;
            _walletService = walletService;
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

            var wallet = await _walletService.GetWalletAsync(
                _walletOptions.WalletConfiguration,
                _walletOptions.WalletCredentials);

            await ProcessAsync(body, wallet);

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(string.Empty);
        }
    }
}