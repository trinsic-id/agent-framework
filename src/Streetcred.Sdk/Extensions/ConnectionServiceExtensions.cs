using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Models.Records;
using Streetcred.Sdk.Models.Records.Search;
using Streetcred.Sdk.Utils;
// ReSharper disable CheckNamespace

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// A collection of convenience methods for the <see cref="ICredentialService"/> class.
    /// </summary>
    public static class ConnectionServiceExtensions
    {
        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are in <see cref="ConnectionState.Negotiating"/> state.
        /// </summary>
        /// <returns>The negotiating connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListNegotiatingConnectionsAsync(
            this IConnectionService connectionService, Wallet wallet, int count = 100)
            => connectionService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, ConnectionState.Negotiating.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are in <see cref="ConnectionState.Connected"/> state.
        /// </summary>
        /// <returns>The connected connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListConnectedConnectionsAsync(
            this IConnectionService connectionService, Wallet wallet, int count = 100)
            => connectionService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, ConnectionState.Connected.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are in <see cref="ConnectionState.Invited"/> state.
        /// </summary>
        /// <returns>The invited connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListInvitedConnectionsAsync(
            this IConnectionService connectionService, Wallet wallet, int count = 100)
            => connectionService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, ConnectionState.Invited.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a <see cref="ConnectionRecord"/> by key.
        /// </summary>
        /// <returns>The connection record.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="myKey">My key.</param>
        public static async Task<ConnectionRecord> GetByMyKey(
            this IConnectionService connectionService, Wallet wallet, string myKey)
            => (await connectionService.ListAsync(wallet,
                new SearchRecordQuery { { TagConstants.MyKey, myKey } }, 1)).FirstOrDefault();
    }
}