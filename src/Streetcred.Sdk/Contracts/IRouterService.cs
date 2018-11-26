using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Models.Records;

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
        /// <param name="connection">The connection record.</param>
        /// <returns></returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection);
    }
}
