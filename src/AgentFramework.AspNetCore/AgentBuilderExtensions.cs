using System;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Wallets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AgentFramework.AspNetCore
{
    /// <summary>
    /// Agent builder extensions.
    /// </summary>
    public static class AgentBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="DefaultAgent"/> service provisioned with <see cref="BasicProvisioningConfiguration"/>
        /// </summary>
        /// <returns>The basic agent.</returns>
        /// <param name="builder">Builder.</param>
        /// <param name="config">Config.</param>
        public static AgentBuilder AddBasicAgent(this AgentBuilder builder, Action<BasicProvisioningConfiguration> config)
        {
            return AddBasicAgent<DefaultAgent>(builder, config);
        }

        /// <summary>
        /// Adds a custom agent service provisioned with <see cref="BasicProvisioningConfiguration"/>
        /// </summary>
        /// <returns>The basic agent.</returns>
        /// <param name="builder">Builder.</param>
        /// <param name="config">Config.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static AgentBuilder AddBasicAgent<T>(this AgentBuilder builder, Action<BasicProvisioningConfiguration> config)
            where T : class, IAgent
        {
            var configuration = new BasicProvisioningConfiguration();
            config?.Invoke(configuration);

            return AddAgent<T, BasicProvisioningConfiguration>(builder, () => configuration);
        }

        /// <summary>
        /// Adds a <see cref="DefaultAgent"/> service provisioned with <see cref="IssuerProvisioningConfiguration"/>
        /// </summary>
        /// <returns>The issuer agent.</returns>
        /// <param name="builder">Builder.</param>
        /// <param name="config">Config.</param>
        public static AgentBuilder AddIssuerAgent(this AgentBuilder builder, Action<IssuerProvisioningConfiguration> config)
        {
            return AddIssuerAgent<DefaultAgent>(builder, config);
        }

        /// <summary>
        /// Adds a custom agent service provisioned with <see cref="IssuerProvisioningConfiguration"/>
        /// </summary>
        /// <returns>The issuer agent.</returns>
        /// <param name="builder">Builder.</param>
        /// <param name="config">Config.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static AgentBuilder AddIssuerAgent<T>(this AgentBuilder builder, Action<IssuerProvisioningConfiguration> config)
            where T : class, IAgent
        {
            var configuration = new IssuerProvisioningConfiguration();
            config?.Invoke(configuration);

            return AddAgent<T, IssuerProvisioningConfiguration>(builder, () => configuration);
        }

        /// <summary>
        /// Adds a custom agent service provisioned with custom <see cref="ProvisioningConfiguration"/>
        /// </summary>
        /// <returns>The agent.</returns>
        /// <param name="builder">Builder.</param>
        /// <param name="createConfiguration">Create configuration.</param>
        /// <typeparam name="TAgent">The 1st type parameter.</typeparam>
        /// <typeparam name="TConfiguration">The 2nd type parameter.</typeparam>
        public static AgentBuilder AddAgent<TAgent, TConfiguration>(this AgentBuilder builder, Func<TConfiguration> createConfiguration)
            where TAgent : class, IAgent
            where TConfiguration : ProvisioningConfiguration
        {
            var configuration = createConfiguration.Invoke();

            builder.Services.Configure<WalletOptions>(obj =>
            {
                obj.WalletConfiguration = configuration.WalletConfiguration;
                obj.WalletCredentials = configuration.WalletCredentials;
            });

            builder.Services.Configure<PoolOptions>(obj =>
            {
                obj.PoolName = configuration.PoolName;
                obj.GenesisFilename = configuration.GenesisFilename;
            });

            builder.Services.AddSingleton<ProvisioningConfiguration>(configuration);
            builder.Services.AddSingleton<IAgent, TAgent>();
            builder.Services.AddSingleton<IHostedService>(s => new AgentHostedService(
                configuration,
                s.GetRequiredService<IProvisioningService>(),
                s.GetRequiredService<IPoolService>(),
                s.GetRequiredService<IOptions<PoolOptions>>()));

            return builder;
        }
    }
}
