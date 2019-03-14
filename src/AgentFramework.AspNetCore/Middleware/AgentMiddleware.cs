using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using Microsoft.AspNetCore.Http;

namespace AgentFramework.AspNetCore.Middleware
{
    /// <summary>
    /// An agent middleware
    /// </summary>
    public class AgentMiddleware : AgentMessageProcessorBase
    {
        private readonly RequestDelegate _next;
        private readonly IAgentContextProvider _contextProvider;

        /// <summary>Initializes a new instance of the <see cref="AgentMiddleware"/> class.</summary>
        /// <param name="next">The next.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="contextProvider">The agent context provider.</param>
        public AgentMiddleware(
            RequestDelegate next,
            IServiceProvider serviceProvider,
            IAgentContextProvider contextProvider)
            : base(serviceProvider)
        {
            _next = next;
            _contextProvider = contextProvider;
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

            var agentContext = await _contextProvider.GetContextAsync();

            var result = await ProcessAsync(body, agentContext);

            context.Response.StatusCode = 200;

            if (result != null)
                await context.Response.Body.WriteAsync(result, 0, result.Length);
            else
                await context.Response.WriteAsync(string.Empty);
        }
    }
}