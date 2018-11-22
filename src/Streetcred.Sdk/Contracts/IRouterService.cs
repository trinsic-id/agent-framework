using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Models;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Router service.
    /// </summary>
    public interface IRouterService
    {
        /// <summary>
        /// Forwards the asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="toKey">The recipient.</param>
        /// <param name="fromKey">The sender.</param>
        /// <returns></returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, string toKey, string fromKey, AgentEndpoint endpoint);
    }
}
