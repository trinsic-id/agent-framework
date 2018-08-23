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
using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Extensions
{
    public class AgentBuilder
    {
        private readonly IApplicationBuilder _app;
        private WalletConfiguration _defaultConfiguration;
        private WalletCredentials _defaultCredentials;

        private string _agentSeed;
        private string _publicUri;

        private List<Task> createTasks;

        internal AgentBuilder(IApplicationBuilder app)
        {
            this._app = app;
            createTasks = new List<Task>();
        }

        /// <summary>
        /// Creates a default pool configuration with the specified file as genesis txn
        /// </summary>
        /// <param name="poolName">Name of the pool.</param>
        /// <param name="genesisFile">Genesis file.</param>
        /// <returns>
        /// The pool configuration.
        /// </returns>
        public AgentBuilder WithPool(string poolName, string genesisFile)
        {
            var poolService = _app.ApplicationServices.GetService<IPoolService>();

            createTasks.Add(poolService.CreatePoolAsync(poolName, genesisFile));
            return this;
        }

        /// <summary>
        /// Creates a default wallet with the configuration and credentials specified
        /// </summary>
        /// <returns>The wallet.</returns>
        /// <param name="configuration">Configuration.</param>
        /// <param name="credentials">Credentials.</param>
        public AgentBuilder WithWallet(WalletConfiguration configuration, WalletCredentials credentials)
        {
            _defaultConfiguration = configuration;
            _defaultCredentials = credentials;

            var walletService = _app.ApplicationServices.GetService<IWalletService>();

            createTasks.Add(walletService.CreateWalletAsync(configuration, credentials));
            return this;
        }

        /// <summary>
        /// Registers an agent endpoint record and generates agent public did and verykey
        /// </summary>
        /// <returns>The public agent.</returns>
        /// <param name="publicUri">Public URI.</param>
        /// <param name="agentSeed">Agent seed used to derive the DID. Leave null to generate
        /// random DID and Verkey</param>
        public AgentBuilder AsPublicAgent(string publicUri, string agentSeed = null)
        {
            this._agentSeed = agentSeed;
            this._publicUri = publicUri;

            return this;
        }

        internal void Initialize()
        {
            var createAgentTask = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(createTasks);
                }
                catch (WalletExistsException)
                {
                    // Wallet already exists, swallow exception
                }
                catch (PoolLedgerConfigExistsException)
                {
                    // Pool already exists, swallow exception
                }

                if (string.IsNullOrEmpty(_publicUri))
                    return;

                if (_defaultCredentials == null || _defaultConfiguration == null)
                {
                    throw new Exception(
                        "Default wallet configuration and credential must be provided to register a public agent");
                }

                var walletService = _app.ApplicationServices.GetService<IWalletService>();
                var endpointService = _app.ApplicationServices.GetService<IEndpointService>();

                var wallet = await walletService.GetWalletAsync(_defaultConfiguration, _defaultCredentials);

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