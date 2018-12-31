using System;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Wallets;
using Hyperledger.Indy.PoolApi;
using Microsoft.Extensions.Options;

namespace AgentFramework.AspNetCore.Configuration
{
    public class AgentBuilder
    {
        private string _issuerSeed;

        private string _agentOwnerName;
        private string _agentOwnerImageUrl;

        internal Uri TailsBaseUri;

        private readonly IPoolService _poolService;
        private readonly IProvisioningService _provisioningService;
        private readonly WalletOptions _walletOptions;
        private readonly PoolOptions _poolOptions;

        public AgentBuilder(
            IPoolService poolService,
            IProvisioningService provisioningService,
            IOptions<WalletOptions> walletOptions,
            IOptions<PoolOptions> poolOptions)
        {
            _poolService = poolService;
            _provisioningService = provisioningService;
            _walletOptions = walletOptions.Value;
            _poolOptions = poolOptions.Value;
        }

        public AgentBuilder AddOwnershipInfo(string name, string imageUrl)
        {
            _agentOwnerName = name;
            _agentOwnerImageUrl = imageUrl;

            return this;
        }

        /// <summary>
        /// Set the issuer seed for generating detemerinistic DID and VerKey. 
        /// </summary>
        /// <param name="issuerSeed">The issuer seed. Leave <c>null</c> to generate a random one.</param>
        /// <returns></returns>
        public AgentBuilder AddIssuer(string issuerSeed = null)
        {
            _issuerSeed = issuerSeed;
            return this;
        }
        
        /// <summary>
        /// Sets the base URI for the tails service
        /// </summary>
        /// <returns>The tails base URI.</returns>
        /// <param name="tailsBaseUri">Tails base URI.</param>
        public AgentBuilder SetTailsBaseUri(string tailsBaseUri)
        {
            TailsBaseUri = new Uri(tailsBaseUri);
            return this;
        }

        internal async Task Build(Uri endpointUri)
        {
            try
            {
                await _poolService.CreatePoolAsync(_poolOptions.PoolName, _poolOptions.GenesisFilename);
            }
            catch (PoolLedgerConfigExistsException)
            {
                // Pool already exists, swallow exception
            }

            try
            {
                var issuerConfig = _issuerSeed != null ? new IssuerAgentConfiguration(_issuerSeed, new Uri(endpointUri, "tails")) : null;

                var agentService = new AgentService
                {
                    ServiceEndpoint = endpointUri.ToString()
                };

                var provisionConfig = new ProvisioningConfiguration(
                    _walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials,
                    new AgentOwner
                    {
                        Name = _agentOwnerName,
                        ImageUrl = _agentOwnerImageUrl
                    },
                    new IDidService[] { agentService },
                    issuerConfig);

                await _provisioningService.ProvisionAgentAsync(provisionConfig);
            }
            catch (AgentFrameworkException ex) when (ex.ErrorCode == ErrorCode.WalletAlreadyProvisioned)
            {
                // Wallet already provisioned
            }
        }
    }
}