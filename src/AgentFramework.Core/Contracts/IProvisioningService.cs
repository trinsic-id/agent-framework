using System;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Wallets;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Provisioning Service.
    /// </summary>
    public interface IProvisioningService
    {
        /// <summary>
        /// Gets my endpoint asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The provisioning record.</returns>
        Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet);

        /// <summary>
        /// Stores the endpoint asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="configuration">The provisioning request.</param>
        /// <returns>The response async.</returns>
        [Obsolete]
        Task ProvisionAgentAsync(Wallet wallet, ProvisioningConfiguration configuration);

        /// <summary>
        /// Creates a wallet and provisions a new agent with the given <see cref="ProvisioningConfiguration" />
        /// </summary>
        /// <param name="configuration">The provisioning configuration.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.WalletAlreadyProvisioned.</exception>
        /// <returns>The response async.</returns>
        Task ProvisionAgentAsync(ProvisioningConfiguration configuration);

        /// <summary>
        /// Adds a did service to the provisioning record.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="service">The service.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The response async.</returns>
        Task AddServiceAsync(Wallet wallet, IDidService service);

        /// <summary>
        /// Updates the specified agent service.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="service">The did service.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The response async.</returns>
        Task UpdateServiceAsync(Wallet wallet, IDidService service);
    }
}
