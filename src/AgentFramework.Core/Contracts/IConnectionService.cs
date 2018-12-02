using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Connection Service.
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection identifier.</param>
        Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId);

        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> items for the given search criteria
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="wallet">The wallet to search</param>
        /// <param name="query">The query used to filter the search results.</param>
        /// <param name="count">The maximum item count of items to return to return.</param>
        Task<List<ConnectionRecord>> ListAsync(Wallet wallet, ISearchQuery query = null, int count = 100);

        /// <summary>
        /// Creates the invitation asynchronous.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="config">An optional configuration object used to configure the resulting invitations presentation</param>
        /// <returns></returns>
        Task<ConnectionInvitationMessage> CreateInvitationAsync(Wallet wallet, InviteConfiguration config = null);

        /// <summary>
        /// Accepts the connection invitation async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="offer">Offer.</param>
        /// <returns>
        /// Connection identifier unique for this connection
        /// </returns>
        Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitationMessage offer);

        /// <summary>
        /// Process the connection request for a given connection async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="request">Request.</param>
        /// <param name="connection">Connection.</param>
        /// <returns>
        /// Connection identifier this requests is related to.
        /// </returns>
        Task<string> ProcessRequestAsync(Wallet wallet, ConnectionRequestMessage request, ConnectionRecord connection);

        /// <summary>
        /// Accepts the connection request and sends a connection response
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <returns></returns>
        Task AcceptRequestAsync(Wallet wallet, string connectionId);

        /// <summary>
        /// Processes the connection response for a given connection async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="response">Response.</param>
        /// <param name="connection">Connection.</param>
        /// <returns>
        /// The response async.
        /// </returns>
        Task ProcessResponseAsync(Wallet wallet, ConnectionResponseMessage response, ConnectionRecord connection);

        /// <summary>
        /// Deletes a connection from the local store
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection Identifier.</param>
        /// <returns>The response async with a boolean indicating if deletion occured sucessfully</returns>
        Task<bool> DeleteAsync(Wallet wallet, string connectionId);
    }
}
