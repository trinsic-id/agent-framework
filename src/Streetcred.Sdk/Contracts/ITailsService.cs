using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace Streetcred.Sdk.Contracts
{
    public interface ITailsService
    {

        /// <summary>
        /// Gets a tail reader handle from local storage.
        /// If <paramref name="pool"/> is specified, retreives the latest public tails file
        /// for the specified <paramref name="revocationRegistryId"/> and stores it locally.
        /// </summary>
        /// <returns>The tails reader async.</returns>
        /// <param name="revocationRegistryId">Revocation registry identifier.</param>
        /// <param name="pool">Pool.</param>
        Task<BlobStorageReader> GetTailsAsync(string revocationRegistryId, Pool pool = null);

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        /// <param name="revocationRegistryId">Storage identifier.</param>
        Task<BlobStorageWriter> CreateTailsAsync(string revocationRegistryId);

        /// <summary>
        /// Retrieves the tails file from a remote URI endpoint
        /// </summary>
        /// <returns>The tails async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="revocationRegistryId">Revocation registry identifier.</param>
        /// <param name="filename">The local tails filename where registry data will be stored.</param>
        Task FetchTailsAsync(Pool pool, string revocationRegistryId, string filename);

        /// <summary>
        /// Gets the tails location URI that will be stored on the ledger with the revocation registry definition.
        /// </summary>
        /// <returns>The tails URI.</returns>
        /// <param name="wallet">The wallet.</param>
        /// <param name="revocationRegistryId">Revocation registry identifier.</param>
        Task<string> FormatTailsLocationAsync(Wallet wallet, string revocationRegistryId);
    }
}