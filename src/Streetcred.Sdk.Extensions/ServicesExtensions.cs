using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Runtime;

namespace Streetcred.Sdk.Extensions
{
    public static class ServicesExtensions
    {
        private static void RegisterCoreServices(this IServiceCollection services)
        {
            services.AddTransient<AgentBuilder>();
            services.AddOptions<WalletOptions>();
            services.AddOptions<PoolOptions>();
        }

        /// <summary>
        /// Adds a sovrin issuer agent with the provided configuration
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="agentConfiguration">The agent configuration.</param>
        /// <param name="serviceConfiguration">The service resolution configuration</param>
        public static void AddAgent(this IServiceCollection services,
            Action<AgentConfiguration> agentConfiguration = null, Action<ServicesBuilder> serviceConfiguration = null)
        {
            RegisterCoreServices(services);

            var serviceBuilder = new ServicesBuilder();
            serviceConfiguration?.Invoke(serviceBuilder);
            serviceBuilder.RegisterServices(ref services);

            var defaultConfiguration = new AgentConfiguration();
            agentConfiguration?.Invoke(defaultConfiguration);

            services.Configure<WalletOptions>((obj) =>
            {
                obj.WalletConfiguration = defaultConfiguration.WalletOptions.WalletConfiguration;
                obj.WalletCredentials = defaultConfiguration.WalletOptions.WalletCredentials;
            });

            services.Configure<PoolOptions>((obj) =>
            {
                obj.PoolName = defaultConfiguration.PoolOptions.PoolName;
                obj.GenesisFilename = defaultConfiguration.PoolOptions.GenesisFilename;
            });
        }

        /// <summary>
        /// Allows default agent configuration
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="endpointUri">The endpointUri.</param>
        /// <param name="agentOptions">Options.</param>
        public static void UseAgent(this IApplicationBuilder app, string endpointUri,
            Action<AgentBuilder> agentOptions = null) => UseAgent<AgentMiddleware>(app, endpointUri, agentOptions);

        /// <summary>
        /// Allows agent configuration by specifyig a custom middleware
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="endpointUri">The endpointUri.</param>
        /// <param name="agentOptions">Options.</param>
        public static void UseAgent<T>(this IApplicationBuilder app, string endpointUri,
            Action<AgentBuilder> agentOptions = null)
        {
            if (string.IsNullOrWhiteSpace(endpointUri)) throw new ArgumentNullException(nameof(endpointUri));

            var agentBuilder = app.ApplicationServices.GetService<AgentBuilder>();

            agentOptions?.Invoke(agentBuilder);

            var endpoint = new Uri(endpointUri);

            agentBuilder.Build(endpoint).GetAwaiter().GetResult();

            app.MapWhen(
                context => context.Request.Path.StartsWithSegments(endpoint.AbsolutePath),
                appBuilder => { appBuilder.UseMiddleware<T>(); });

            if (agentBuilder.TailsBaseUri != null)
            {
                var tailsEndpoint = new Uri(agentBuilder.TailsBaseUri);

                app.MapWhen(
                    context => context.Request.Path.StartsWithSegments(tailsEndpoint.AbsolutePath),
                    appBuilder => { appBuilder.UseMiddleware<TailsMiddleware>(); });
            }
        }
    }
}