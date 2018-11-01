using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Contracts
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
