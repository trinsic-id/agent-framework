using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Sovrin.Agents.Model;

namespace Streetcred.Sdk.Contracts
{
    public interface IEndpointService
    {
        /// <summary>
        /// Gets my endpoint asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <returns></returns>
        Task<AgentEndpoint> GetEndpointAsync(Wallet wallet);

        /// <summary>
        /// Stores the endpoint asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        Task StoreEndpointAsync(Wallet wallet, AgentEndpoint endpoint);
    }
}
