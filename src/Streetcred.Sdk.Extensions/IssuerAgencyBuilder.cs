using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Extensions
{
    public class IssuerAgencyBuilder
    {
        private string _issuerSeed;
        private string _agentSeed;
        private string _publicUri = "http://localhost:5000/";

        private List<Task> createTasks;
        private readonly IWalletService walletService;
        private readonly IPoolService poolService;
        private readonly IEndpointService endpointService;

        internal IssuerAgencyBuilder(IWalletService walletService,
                              IPoolService poolService,
                              IEndpointService endpointService)
        {
            createTasks = new List<Task>();
            this.walletService = walletService;
            this.poolService = poolService;
            this.endpointService = endpointService;
        }

        public IssuerAgencyBuilder WithIssuerSeed(string issuerSeed)
        {
            _issuerSeed = issuerSeed;

            return this;
        }

        public IssuerAgencyBuilder WithAgentSeed(string agentSeed)
        {
            _agentSeed = agentSeed;

            return this;
        }

        internal void Initialize(WalletOptions walletOptions, PoolOptions poolOptions)
        {
            var createAgentTask = Task.Run(async () =>
            {
                try
                {
                    await walletService.CreateWalletAsync(walletOptions.WalletConfiguration, walletOptions.WalletCredentials);
                }
                catch (WalletExistsException)
                {
                    // Wallet already exists, swallow exception
                }
                try
                {
                    await poolService.CreatePoolAsync(poolOptions.PoolName, poolOptions.GenesisFilename);
                }
                catch (PoolLedgerConfigExistsException)
                {
                    // Pool already exists, swallow exception
                }

                if (string.IsNullOrEmpty(_publicUri))
                    return;

                var wallet = await walletService.GetWalletAsync(walletOptions.WalletConfiguration,
                                                                walletOptions.WalletCredentials);

                var agent = await Did.CreateAndStoreMyDidAsync(
                    wallet, _agentSeed == null
                        ? "{}"
                        : JsonConvert.SerializeObject(new { seed = _agentSeed }));

                await endpointService.StoreEndpointAsync(wallet, new AgentEndpoint
                {
                    Uri = _publicUri,
                    Did = agent.Did,
                    Verkey = agent.VerKey
                });
            });

            createAgentTask.Wait();
        }
    }
}