using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Contracts
{
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
        /// <param name="provisioningRequest">The provisioning request.</param>
        /// <param name="createIssuer">if set to <c>true</c> [create issuer].</param>
        /// <returns></returns>
        Task ProvisionAgentAsync(Wallet wallet, ProvisioningRequest provisioningRequest);
    }
}
