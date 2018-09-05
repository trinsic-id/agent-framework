using System;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Extensions
{
    public class IssuerAgencyBuilder
    {
        private string _issuerSeed;
        private string _agentSeed;
        private string _publicUri = "http://localhost:5000/";
        
        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;
        private readonly IProvisioningService _provisioningService;

        internal IssuerAgencyBuilder(IWalletService walletService,
                              IPoolService poolService,
                              IProvisioningService provisioningService)
        {
            this._walletService = walletService;
            this._poolService = poolService;
            this._provisioningService = provisioningService;
        }
        /// <summary>
        /// Set the issuer seed for generating detemerinistic DID and VerKey
        /// </summary>
        /// <param name="issuerSeed">The issuer seed.</param>
        /// <returns></returns>
        public IssuerAgencyBuilder WithIssuerSeed(string issuerSeed)
        {
            _issuerSeed = issuerSeed;

            return this;
        }

        /// <summary>
        /// Set the agent seed for generating detemerinistic DID and VerKey
        /// </summary>
        /// <param name="agentSeed">The agent seed.</param>
        /// <returns></returns>
        public IssuerAgencyBuilder WithAgentSeed(string agentSeed)
        {
            _agentSeed = agentSeed;

            return this;
        }

        /// <summary>
        /// Sets the agent endpoint uri
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public IssuerAgencyBuilder WithEndpoint(string uri)
        {
            _publicUri = uri;

            return this;
        }

        internal void Build(WalletOptions walletOptions, PoolOptions poolOptions)
        {
            var createAgentTask = Task.Run(async () =>
            {
                try
                {
                    await _walletService.CreateWalletAsync(walletOptions.WalletConfiguration,
                        walletOptions.WalletCredentials);
                }
                catch (WalletExistsException)
                {
                    // Wallet already exists, swallow exception
                }

                try
                {
                    await _poolService.CreatePoolAsync(poolOptions.PoolName, poolOptions.GenesisFilename);
                }
                catch (PoolLedgerConfigExistsException)
                {
                    // Pool already exists, swallow exception
                }

                var wallet = await _walletService.GetWalletAsync(walletOptions.WalletConfiguration,
                    walletOptions.WalletCredentials);

                await _provisioningService.ProvisionAgentAsync(wallet, new ProvisioningRequest
                {
                    AgentSeed = _agentSeed,
                    EndpointUri = new Uri(_publicUri),
                    CreateIssuer = true
                });
            });

            createAgentTask.Wait();
        }
    }
}