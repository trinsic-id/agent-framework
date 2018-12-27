using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
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
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null);

        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, byte[] message, ConnectionRecord connection,
            string recipientKey = null);

        /// <summary>
        /// Get a route record by its identifier.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="id">Identifier of the record.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The record async.</returns>
        Task<RouteRecord> GetRoute(Wallet wallet, string id);

        /// <summary>
        /// Get a list of routing records in the current wallet.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="connectionId">[Optional] The connection id to filter the results to.</param>
        /// <returns>A list of routing records async.</returns>
        Task<IList<RouteRecord>> GetRoutesAsync(Wallet wallet, string connectionId = null);

        /// <summary>
        /// Processes a forward message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The response async.</returns>
        Task ProcessForwardMessageAsync(Wallet wallet, ForwardMessage message);

        /// <summary>
        /// Processes a create route message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <returns>The response async.</returns>
        Task ProcessCreateRouteMessageAsync(Wallet wallet, CreateRouteMessage message, ConnectionRecord connection);

        /// <summary>
        /// Processes a delete route message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.InvalidOperation.</exception>
        /// <returns>The response async.</returns>
        Task ProcessDeleteRouteMessageAsync(Wallet wallet, DeleteRouteMessage message, ConnectionRecord connection);
    }
}
