using System;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentFramework.AspNetCore.Middleware
{
    /// <summary>An agent middleware</summary>
    public class AgentMiddleware : AgentBase
    {
        private readonly RequestDelegate _next;
        private readonly IWalletService _walletService;
        private readonly WalletOptions _walletOptions;

        /// <summary>Initializes a new instance of the <see cref="AgentMiddleware"/> class.</summary>
        /// <param name="next">The next.</param>
        /// <param name="walletService">The wallet service.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="walletOptions">The wallet options.</param>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="logger">The logger.</param>
        public AgentMiddleware(
            RequestDelegate next,
            IWalletService walletService,
            IServiceProvider serviceProvider,
            IOptions<WalletOptions> walletOptions,
            IConnectionService connectionService,
            ILogger<AgentBase> logger)
            : base(serviceProvider, connectionService, logger)
        {
            _next = next;
            _walletService = walletService;
            _walletOptions = walletOptions.Value;
        }

        /// <summary>Called by the ASPNET Core runtime</summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Empty content length</exception>
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