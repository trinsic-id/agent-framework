using System;
using AgentFramework.Core.Contracts;
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
        public static AgentBuilder AddBasicAgent(this AgentBuilder builder, Action<BasicProvisioningConfiguration> config)
        {
            return AddBasicAgent<DefaultAgent>(builder, config);
        }

        public static AgentBuilder AddBasicAgent<T>(this AgentBuilder builder, Action<BasicProvisioningConfiguration> config)
            where T : class, IAgent
        {
            var configuration = new BasicProvisioningConfiguration();
            config?.Invoke(configuration);

            return AddAgent<T, BasicProvisioningConfiguration>(builder, () => configuration);
        }

        public static AgentBuilder AddIssuerAgent(this AgentBuilder builder, Action<IssuerProvisioningConfiguration> config)
        {
            return AddIssuerAgent<DefaultAgent>(builder, config);
        }

        public static AgentBuilder AddIssuerAgent<T>(this AgentBuilder builder, Action<IssuerProvisioningConfiguration> config)
            where T : class, IAgent
        {
            var configuration = new IssuerProvisioningConfiguration();
            config?.Invoke(configuration);

            return AddAgent<T, IssuerProvisioningConfiguration>(builder, () => configuration);
        }

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

            builder.Services.AddSingleton<IAgent, TAgent>();
            builder.Services.AddSingleton<IHostedService>(s => new AgentHostedService(
                s.GetRequiredService<IProvisioningService>(),
                configuration,
                s.GetRequiredService<IPoolService>(),
                s.GetRequiredService<IOptions<PoolOptions>>()));

            return builder;
        }
    }
}
