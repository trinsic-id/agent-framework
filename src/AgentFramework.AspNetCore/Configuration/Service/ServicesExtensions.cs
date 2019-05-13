using System;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Wallets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgentFramework.AspNetCore.Configuration.Service
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods
    /// </summary>
    public static class ServicesExtensions
    {
        /// <summary>
        /// Registers the agent framework required services with basic provisioning configuration
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddAgentFramework(this IServiceCollection services)
        {
            AddAgentFramework(services, () => new BasicProvisioningConfiguration());
        }

        public static void AddAgentFramework(this IServiceCollection services, 
            Action<AgentConfigurationBuilder> builder, 
            Func<ProvisioningConfiguration> configurationDelegate = null)
        {
            AddAgentFramework(services, configurationDelegate ?? (() => new BasicProvisioningConfiguration()));
            builder?.Invoke(new AgentConfigurationBuilder(services));
        }

        /// <summary>
        /// Registers the agent framework required services with custom provisioning configuration
        /// </summary>
        /// <param name="services">Services.</param>
        /// <param name="configurationDelegate">Configuration delegate.</param>
        public static void AddAgentFramework(this IServiceCollection services, Func<ProvisioningConfiguration> configurationDelegate)
        {
            services.AddOptions<WalletOptions>();
            services.AddOptions<PoolOptions>();
            services.AddSingleton<IHostedService, AgentHostedService>();
            services.AddDefaultMessageHandlers();
            services.AddLogging();

            var configuration = configurationDelegate?.Invoke() ?? new BasicProvisioningConfiguration();
            services.AddSingleton(configuration);

            services.AddDefaultServices();
            services.AddDefaultMessageHandlers();

            services.Configure<WalletOptions>(obj =>
            {
                obj.WalletConfiguration = configuration.WalletConfiguration;
                obj.WalletCredentials = configuration.WalletCredentials;
            });

            services.Configure<PoolOptions>(obj =>
            {
                obj.PoolName = configuration.PoolName;
                obj.GenesisFilename = configuration.GenesisFilename;
            });
        }

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