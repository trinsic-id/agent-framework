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
    public class AgentMiddleware : IMiddleware
    {
        private readonly IAgentFactory _agentFactory;
        private readonly IAgentContextProvider _contextProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.AspNetCore.Middleware.AgentMiddleware"/> class.
        /// </summary>
        /// <param name="agentFactory">Agent factory.</param>
        /// <param name="contextProvider">Context provider.</param>
        public AgentMiddleware(
             IAgentFactory agentFactory,
             IAgentContextProvider contextProvider)
        {
            _agentFactory = agentFactory;
            _contextProvider = contextProvider;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                && !context.Request.ContentType.Equals(DefaultMessageService.AgentWireMessageMimeType))
            {
                await next(context);
                return;
            }

            if (context.Request.ContentLength == null) throw new Exception("Empty content length");

            var agent = _agentFactory.Create<IAgent>();

            using (var stream = new StreamReader(context.Request.Body))
            {
                var body = await stream.ReadToEndAsync();

                var result = await agent.ProcessAsync(
                    context: await _contextProvider.GetContextAsync(), //TODO assumes all recieved messages are packed 
                    messageContext: new MessageContext(body.GetUTF8Bytes(), true));

                context.Response.StatusCode = 200;

                if (result != null)
                {
                    context.Response.ContentType = DefaultMessageService.AgentWireMessageMimeType;
                    await result.Stream.CopyToAsync(context.Response.Body);
                }
                else
                    await context.Response.WriteAsync(string.Empty);
            }
        }
    }
}