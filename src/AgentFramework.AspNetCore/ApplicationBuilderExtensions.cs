using AgentFramework.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.AspNetCore
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Allows default agent configuration
        /// </summary>
        /// <param name="app">App.</param>
        public static void UseAgentFramework(this IApplicationBuilder app) => UseAgentFramework<AgentMiddleware>(app);

        /// <summary>
        /// Allows agent configuration by specifying a custom middleware
        /// </summary>
        /// <param name="app">App.</param>
        public static void UseAgentFramework<T>(this IApplicationBuilder app) => app.UseMiddleware<T>();
    }
}