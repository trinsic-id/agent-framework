using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Wallets;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AgentFramework.AspNetCore
{
    /// <summary>
    /// Agent hosted service.
    /// </summary>
    public class AgentHostedService : IHostedService
    {
        private readonly IProvisioningService _provisioningService;
        private readonly IPoolService _poolService;
        private readonly PoolOptions _poolOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.AspNetCore.AgentHostedService"/> class.
        /// </summary>
        /// <param name="provisioningService">Provisioning service.</param>
        /// <param name="provisioningConfiguration">Provisioning configuration.</param>
        /// <param name="poolService">Pool service.</param>
        /// <param name="poolOptions">Pool options.</param>
        public AgentHostedService(
            IProvisioningService provisioningService,
            ProvisioningConfiguration provisioningConfiguration,
            IPoolService poolService,
            IOptions<PoolOptions> poolOptions)
        {
            _provisioningService = provisioningService;
            ProvisioningConfiguration = provisioningConfiguration;
            _poolService = poolService;
            _poolOptions = poolOptions.Value;
        }

        /// <summary>
        /// Gets the provisioning configuration.
        /// </summary>
        /// <value>The provisioning configuration.</value>
        public ProvisioningConfiguration ProvisioningConfiguration { get; }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_poolOptions.GenesisFilename != null)
                    await _poolService.CreatePoolAsync(_poolOptions.PoolName, _poolOptions.GenesisFilename);
            }
            catch (PoolLedgerConfigExistsException)
            {
                // Pool already exists, swallow exception
            }

            try
            {
                await _provisioningService.ProvisionAgentAsync(ProvisioningConfiguration);
            }
            catch (WalletExistsException)
            {
                // Wallet already exists, swallow exception
            }
            catch (AgentFrameworkException ex) when (ex.ErrorCode == ErrorCode.WalletAlreadyProvisioned)
            {
                // Wallet already provisioned
            }
            catch (WalletStorageException)
            {
                // Aggregate exception thrown when using custom wallets

                // TODO: TM: add support to Indy SDK to expose exception types
            }
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
