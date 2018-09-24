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
        /// If <paramref name="pool"/> is specified, retreives the latest public tails file
        /// for the specified <paramref name="revocationRegistryId"/> and stores it locally.
        /// </summary>
        /// <returns>The tails reader async.</returns>
        /// <param name="revocationRegistryId">Revocation registry identifier.</param>
        /// <param name="pool">Pool.</param>
        Task<BlobStorageReader> OpenTailsAsync(string revocationRegistryId, Pool pool = null);

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        /// <param name="revocationRegistryId">Storage identifier.</param>
        Task<BlobStorageWriter> CreateTailsAsync(string revocationRegistryId);
    }
}