using System.Threading.Tasks;
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
        /// <returns></returns>
        Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet);

        /// <summary>
        /// Stores the endpoint asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="provisioningConfiguration">The provisioning request.</param>
        /// <returns></returns>
        Task ProvisionAgentAsync(Wallet wallet, ProvisioningConfiguration provisioningConfiguration);
    }
}
