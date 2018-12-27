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
                                         AgentOwner ownershipInfo = null,
                                         IDidService[] services = null,
                                         IssuerAgentConfiguration issuerConfiguration = null)
        {
            WalletConfiguration = walletConfig;
            WalletCredentials = walletCredentials;
            OwnershipInfo = ownershipInfo;
            AgentServices = services;
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
