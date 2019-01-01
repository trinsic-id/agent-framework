using System;
using AgentFramework.Core.Models.Did;

namespace AgentFramework.Core.Models.Wallets
{
    /// <summary>
    /// A configuration object for controlling the provisioning of a new agent.
    /// </summary>
    public class ProvisioningConfiguration
    {
        public ProvisioningConfiguration(WalletConfiguration walletConfig,
                                         WalletCredentials walletCredentials,
                                         IDidService[] services,
                                         AgentOwner ownershipInfo = null,
                                         IssuerAgentConfiguration issuerConfiguration = null)
        {
            if (services == null || services.Length == 0)
                throw new ArgumentNullException(nameof(services),
                    "At least one agent service must be supplied for the agent");

            WalletConfiguration = walletConfig ?? throw new ArgumentNullException(nameof(walletConfig),
                                      "Wallet configuration must be specified");
            WalletCredentials = walletCredentials ?? throw new ArgumentNullException(nameof(walletCredentials),
                    "Wallet redentials must be specified");

            OwnershipInfo = ownershipInfo;
            AgentServices = services;
            IssuerAgentConfiguration = issuerConfiguration;
        }

        public ProvisioningConfiguration(WalletConfiguration walletConfig,
            WalletCredentials walletCredentials,
            IDidService service,
            AgentOwner ownershipInfo = null,
            IssuerAgentConfiguration issuerConfiguration = null)
        {
            WalletConfiguration = walletConfig ?? throw new ArgumentNullException(nameof(walletConfig),
                                      "Wallet configuration must be specified");
            WalletCredentials = walletCredentials ?? throw new ArgumentNullException(nameof(walletCredentials),
                                    "Wallet redentials must be specified");

            if (service == null)
                throw new ArgumentNullException(nameof(service),
                    "An agent service must be specified");

            OwnershipInfo = ownershipInfo;
            AgentServices = new[] { service };
            IssuerAgentConfiguration = issuerConfiguration;
        }

        /// <summary>
        /// Gets or sets the wallet configuration.
        /// </summary>
        /// <value>
        /// The wallet configuration.
        /// </value>
        public WalletConfiguration WalletConfiguration { get; }

        /// <summary>
        /// Gets or sets the wallet credentials.
        /// </summary>
        /// <value>
        /// The wallet credentials.
        /// </value>
        public WalletCredentials WalletCredentials { get; }

        /// <summary>
        /// Gets or sets the ownership info of the agent
        /// </summary>
        /// <value>
        /// The agent ownership info
        /// </value>
        public AgentOwner OwnershipInfo { get; }

        /// <summary>
        /// Gets or sets the issuer agent configuration
        /// </summary>
        /// <value>
        /// The issuer agent configuration.
        /// </value>
        public IssuerAgentConfiguration IssuerAgentConfiguration { get; }

        /// <summary>
        /// Gets or sets the agent services.
        /// </summary>
        /// <value>
        /// The agent services.
        /// </value>
        public IDidService[] AgentServices { get; }
    }
}
