using System;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.AspNetCore.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace AgentFramework.AspNetCore.Configuration.Service
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods
    /// </summary>
    public static class ServicesExtensions
    {
        private static void RegisterCoreServices(this IServiceCollection services)
        {
            services.AddTransient<AgentBuilder>();
            services.AddOptions<WalletOptions>();
            services.AddOptions<PoolOptions>();
        }

        /// <summary>
        /// Adds a Sovrin issuer agent with the provided configuration
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="agentConfiguration">The agent configuration.</param>
        public static void AddAgentFramework(this IServiceCollection services,
            Action<AgentConfigurationBuilder> agentConfiguration = null)
        {
            RegisterCoreServices(services);

            var serviceBuilder = new AgentConfigurationBuilder(services);
            agentConfiguration?.Invoke(serviceBuilder);

            serviceBuilder.AddDefaultServices();

            services = serviceBuilder.Services;

            services.Configure<WalletOptions>(obj =>
            {
                obj.WalletConfiguration = serviceBuilder.WalletOptions.WalletConfiguration;
                obj.WalletCredentials = serviceBuilder.WalletOptions.WalletCredentials;
            });

            services.Configure<PoolOptions>(obj =>
            {
                obj.PoolName = serviceBuilder.PoolOptions.PoolName;
                obj.GenesisFilename = serviceBuilder.PoolOptions.GenesisFilename;
            });
        }

        /// <summary>
        /// Allows default agent configuration
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="endpointUri">The endpointUri.</param>
        /// <param name="agentOptions">Options.</param>
        public static void UseAgentFramework(this IApplicationBuilder app, string endpointUri,
            Action<AgentBuilder> agentOptions = null) => UseAgentFramework<AgentMiddleware>(app, endpointUri, agentOptions);

        /// <summary>
        /// Allows agent configuration by specifying a custom middleware
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="endpointUri">The endpointUri.</param>
        /// <param name="agentOptions">Options.</param>
        public static void UseAgentFramework<T>(this IApplicationBuilder app, string endpointUri,
            Action<AgentBuilder> agentOptions = null)
        {
            if (string.IsNullOrWhiteSpace(endpointUri)) throw new ArgumentNullException(nameof(endpointUri));

            var agentBuilder = app.ApplicationServices.GetService<AgentBuilder>();

            agentOptions?.Invoke(agentBuilder);

            var endpoint = new Uri(endpointUri);

            agentBuilder.Build(endpoint).GetAwaiter().GetResult();

            app.MapWhen(
                context => context.Request.Path.Value.StartsWith(endpoint.AbsolutePath),
                appBuilder => { appBuilder.UseMiddleware<T>(); });

            if (agentBuilder.TailsBaseUri != null)
            {
                app.MapWhen(
                    context => context.Request.Path.Value.StartsWith(agentBuilder.TailsBaseUri.AbsolutePath),
                    appBuilder => { appBuilder.UseMiddleware<TailsMiddleware>(); });
            }
        }
    }
}