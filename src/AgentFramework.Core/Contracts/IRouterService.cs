using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
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
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <returns></returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null);
    }
}
