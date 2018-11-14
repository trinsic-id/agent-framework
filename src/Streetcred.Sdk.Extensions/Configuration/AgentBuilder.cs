using System;
using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Options;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Exceptions;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Models.Wallets;

namespace Streetcred.Sdk.Extensions.Configuration
{
    public class AgentBuilder
    {
        private string _issuerSeed;
        private string _agentSeed;

        private string _agentOwnerName;
        private string _agentOwnerImageUrl;

        internal Uri TailsBaseUri;

        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;
        private readonly IProvisioningService _provisioningService;
        private readonly WalletOptions _walletOptions;
        private readonly PoolOptions _poolOptions;
        private bool _createIssuer;

        public AgentBuilder(
            IWalletService walletService,
            IPoolService poolService,
            IProvisioningService provisioningService,
            IOptions<WalletOptions> walletOptions,
            IOptions<PoolOptions> poolOptions)
        {
            _walletService = walletService;
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
            _createIssuer = true;

            return this;
        }

        /// <summary>
        /// Set the agent seed for generating detemerinistic DID and VerKey. Leave <c>null</c> to generate a random one.
        /// </summary>
        /// <param name="agentSeed">The agent seed.</param>
        /// <returns></returns>
        public AgentBuilder SetAgentSeed(string agentSeed)
        {
            _agentSeed = agentSeed;

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
                await _walletService.CreateWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials);
            }
            catch (WalletExistsException)
            {
                // Wallet already exists, swallow exception
            }

            try
            {
                await _poolService.CreatePoolAsync(_poolOptions.PoolName, _poolOptions.GenesisFilename);
            }
            catch (PoolLedgerConfigExistsException)
            {
                // Pool already exists, swallow exception
            }

            var wallet = await _walletService.GetWalletAsync(
                _walletOptions.WalletConfiguration,
                _walletOptions.WalletCredentials);

            try
            {
                await _provisioningService.ProvisionAgentAsync(wallet, new ProvisioningConfiguration
                {
                    AgentSeed = _agentSeed,
                    EndpointUri = endpointUri,
                    CreateIssuer = _createIssuer,
                    IssuerSeed = _issuerSeed,
                    OwnerName = _agentOwnerName,
                    OwnerImageUrl = _agentOwnerImageUrl,
                    TailsBaseUri = TailsBaseUri ?? new Uri(endpointUri, "tails")
                });
            }
            catch (StreetcredSdkException ex) when (ex.ErrorCode == ErrorCode.WalletAlreadyProvisioned)
            {
                // Wallet already provisioned
            }
        }
    }
}