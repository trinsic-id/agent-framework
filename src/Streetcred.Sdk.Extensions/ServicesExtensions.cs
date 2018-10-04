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
        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<IRouterService, RouterService>();
            services.AddSingleton<ITailsService, DefaultTailsService>();
            services.AddSingleton<ISchemaService, DefaultSchemaService>();
            services.AddSingleton<ILedgerService, DefaultLedgerService>();
            services.AddSingleton<IWalletService, DefaultWalletService>();
            services.AddSingleton<IPoolService, DefaultPoolService>();
            services.AddSingleton<IWalletRecordService, DefaultWalletRecordService>();
            services.AddSingleton<IConnectionService, DefaultConnectionService>();
            services.AddSingleton<IMessageSerializer, DefaultMessageSerializer>();
            services.AddSingleton<ICredentialService, DefaultCredentialService>();
            services.AddSingleton<IProvisioningService, DefaultProvisioningService>();
            services.AddTransient<AgentBuilder>();
            services.AddOptions<WalletOptions>();
            services.AddOptions<PoolOptions>();
        }

        /// <summary>
        /// Adds a sovrin issuer agent with the provided configuration
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="agentConfiguration">The agent configuration.</param>
        public static void AddAgent(this IServiceCollection services,
            Action<AgentConfiguration> agentConfiguration = null)
        {
            RegisterServices(services);

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
        /// <param name="options">Options.</param>
        public static void UseAgent(this IApplicationBuilder app, string endpointUri,
            Action<AgentBuilder> options = null) => UseAgent<AgentMiddleware>(app, endpointUri, options);

        /// <summary>
        /// Allows agent configuration by specifyig a custom middleware
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="endpointUri">The endpointUri.</param>
        /// <param name="options">Options.</param>
        public static void UseAgent<T>(this IApplicationBuilder app, string endpointUri,
            Action<AgentBuilder> options = null)
        {
            if (string.IsNullOrWhiteSpace(endpointUri)) throw new ArgumentNullException(nameof(endpointUri));

            var builder = app.ApplicationServices.GetService<AgentBuilder>();

            options?.Invoke(builder);

            var endpoint = new Uri(endpointUri);

            builder.Build(endpoint).GetAwaiter().GetResult();

            app.MapWhen(
                context => context.Request.Path.StartsWithSegments(endpoint.AbsolutePath),
                appBuilder => { appBuilder.UseMiddleware<T>(); });

            if (builder.TailsBaseUri != null)
            {
                var tailsEndpoint = new Uri(builder.TailsBaseUri);

                app.MapWhen(
                    context => context.Request.Path.StartsWithSegments(tailsEndpoint.AbsolutePath),
                    appBuilder => { appBuilder.UseMiddleware<TailsMiddleware>(); });
            }
        }
    }
}