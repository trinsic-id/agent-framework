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
        /// Sends a create route message to a message router.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientIdentifier">The recipient identifier to base the new route off.</param>
        /// <param name="routerConnection">The connection record for the router.</param>
        /// <returns>The response async.</returns>
        Task SendCreateMessageRoute(Wallet wallet, string recipientIdentifier, ConnectionRecord routerConnection);

        /// <summary>
        /// Sends a delete route message to a message router.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientIdentifier">The recipient identifier of the existing route.</param>
        /// <param name="routerConnection">The connection record for the router.</param>
        /// <returns>The response async.</returns>
        Task SendDeleteMessageRoute(Wallet wallet, string recipientIdentifier, ConnectionRecord routerConnection);

        /// <summary>
        /// Get a route record by its identifier.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="id">Identifier of the record.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The record async.</returns>
        Task<RouteRecord> GetRouteRecordAsync(Wallet wallet, string id);

        /// <summary>
        /// Get a list of routing records in the current wallet.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="connectionId">[Optional] The connection id to filter the results to.</param>
        /// <returns>A list of routing records async.</returns>
        Task<IList<RouteRecord>> GetRoutesRecordsAsync(Wallet wallet, string connectionId = null);

        /// <summary>
        /// Creates a routing record in the current wallet.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientIdentifier">The recipient identifier for the routing record.</param>
        /// <param name="connectionId">The connection id linked to the routing record.</param>
        /// <returns>The response async.</returns>
        Task CreateRouteRecordAsync(Wallet wallet, string recipientIdentifier, string connectionId);

        /// <summary>
        /// Deletes a routing record from the current wallet.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientIdentifier">The recipient identifier of the routing record.</param>
        /// <returns>The response async.</returns>
        Task DeleteRouteRecordAsync(Wallet wallet, string recipientIdentifier);

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

        /// <summary>
        /// Packs a forward message for the supplied message and recipients
        /// </summary>
        /// <param name="verkey">Verkey of the intermidiate recipient.</param>
        /// <param name="message">Encrypted message for the recipients.</param>
        /// <param name="recipientIdentifier">Recipient identifiers of the inner forward message.</param>
        /// <returns></returns>
        Task<byte[]> PackForwardMessage(string verkey, byte[] message, string recipientIdentifier);
    }
}
