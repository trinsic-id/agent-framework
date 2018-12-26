using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
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
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null);

        /// <summary>
        /// Forwards the asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, byte[] message, ConnectionRecord connection,
            string recipientKey = null);

        /// <summary>
        /// Processes a forward message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response async.</returns>
        Task ProcessForwardMessage(Wallet wallet, ForwardMessage message);

        /// <summary>
        /// Processes a create route message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <returns>The response async.</returns>
        Task ProcessCreateRouteMessage(Wallet wallet, CreateRouteMessage message, ConnectionRecord connection);

        /// <summary>
        /// Processes a delete route message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <returns>The response async.</returns>
        Task ProcessDeleteRouteMessage(Wallet wallet, DeleteRouteMessage message, ConnectionRecord connection);
    }
}
