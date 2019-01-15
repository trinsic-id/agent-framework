using System;
using AgentFramework.AspNetCore.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.AspNetCore.Configuration
{
    /// <summary>
    /// Agent Configuration Builder
    /// </summary>
    public class AgentConfigurationBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        internal AgentConfigurationBuilder(IServiceCollection services)
        {
            WalletOptions = new WalletOptions();
            PoolOptions = new PoolOptions();
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Wallet options for the agent configuration.
        /// </summary>
        internal WalletOptions WalletOptions { get; private set; }

        /// <summary>
        /// Pool options for the agent configuration.
        /// </summary>
        internal PoolOptions PoolOptions { get; private set; }

        /// <summary>
        /// Flag indicating if the builder should register the default message handlers.
        /// </summary>
        public bool RegisterCoreMessageHandlers { get; internal set; } = true;

        /// <summary>
        /// Services collection 
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Sets the <see cref="WalletOptions" /> for this agent
        /// </summary>
        /// <param name="walletOptions">The wallet options.</param>
        /// <returns></returns>
        public AgentConfigurationBuilder SetWalletOptions(WalletOptions walletOptions)
        {
            WalletOptions = walletOptions;

            return this;
        }

        /// <summary>
        /// Sets the <see cref="PoolOptions"/> for this agent
        /// </summary>
        /// <param name="poolOptions">The pool options.</param>
        /// <returns></returns>
        public AgentConfigurationBuilder SetPoolOptions(PoolOptions poolOptions)
        {
            PoolOptions = poolOptions;

            return this;
        }
    }
}
