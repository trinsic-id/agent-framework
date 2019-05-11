using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
        public AgentMiddleware(
            RequestDelegate next,
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _next = next;
            _contextProvider = serviceProvider.GetRequiredService<IAgentContextProvider>();
        }

        /// <summary>Called by the ASPNET Core runtime</summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Empty content length</exception>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                && !context.Request.ContentType.Equals(DefaultMessageService.AgentWireMessageMimeType))
            {
                await _next(context);
                return;
            }

            if (context.Request.ContentLength == null) throw new Exception("Empty content length");

            using (var stream = new StreamReader(context.Request.Body))
            {
                var body = await stream.ReadToEndAsync();

                var result = await ProcessAsync(
                    context: await _contextProvider.GetContextAsync(), //TODO assumes all recieved messages are packed 
                    messageContext: new MessageContext(body.GetUTF8Bytes(), true));

                context.Response.StatusCode = 200;

                if (result != null)
                {
                    context.Response.ContentType = DefaultMessageService.AgentWireMessageMimeType;
                    await context.Response.Body.WriteAsync(result, 0, result.Length);
                }
                else
                    await context.Response.WriteAsync(string.Empty);
            }
        }
    }
}