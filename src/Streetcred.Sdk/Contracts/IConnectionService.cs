using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Records;

namespace Streetcred.Sdk.Contracts
{
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
        /// Lists the async.
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <returns>
        /// The async.
        /// </returns>
        Task<List<ConnectionRecord>> ListAsync(Wallet wallet);

        /// <summary>
        /// Creates the invitation asynchronous.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <param name="config">An optional configuration object used to configure the resulting invitations presentation</param>
        /// <returns></returns>
        Task<ConnectionInvitation> CreateInvitationAsync(Wallet wallet, string connectionId);

        /// <summary>
        /// Accepts the invitation async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="offer">Offer.</param>
        /// <returns>
        /// Connection identifier unique for this connection
        /// </returns>
        Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitation offer);

        /// <summary>
        /// Accepts the request async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="request">Request.</param>
        /// <returns>
        /// Connection identifier this requests is related to.
        /// </returns>
        Task<string> StoreRequestAsync(Wallet wallet, ConnectionRequest request);

        /// <summary>
        /// Accepts the connection request and sends a connection response
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <returns></returns>
        Task AcceptRequestAsync(Wallet wallet, string connectionId);

        /// <summary>
        /// Accepts the response async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="response">Response.</param>
        /// <returns>
        /// The response async.
        /// </returns>
        Task AcceptResponseAsync(Wallet wallet, ConnectionResponse response);

        /// <summary>
        /// Deletes a connection from the local store
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="connectionId">Connection Identifier.</param>
        /// <returns>The response async with a boolean indicating if deletion occured sucessfully</returns>
        Task<bool> DeleteAsync(Wallet wallet, string connectionId);
    }
}
