using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace Streetcred.Sdk.Contracts
{
    public interface ITailsService
    {

        /// <summary>
        /// Opens an existing tails file and returns a handle.
        /// </summary>
        /// <returns>The tails reader async.</returns>
        /// <param name="filename">The tails filename.</param>
        Task<BlobStorageReader> OpenTailsAsync(string filename);

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        Task<BlobStorageWriter> CreateTailsAsync();

        /// <summary>
        /// Check if the tails filename exists locally and download latest version if it doesn't.
        /// </summary>
        /// <returns>The tails exists async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="revocationRegistryId">Revocation registry identifier.</param>
        Task EnsureTailsExistsAsync(Pool pool, string revocationRegistryId);
    }
}