using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;

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
        Task<BlobStorageReader> GetTailsReaderAsync(string revocationRegistryId, Pool pool = null);

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        /// <param name="revocationRegistryId">Storage identifier.</param>
        Task<BlobStorageWriter> GetTailsWriterAsync(string revocationRegistryId);

        /// <summary>
        /// Fetchs the tails file from a remote URI endpoint
        /// </summary>
        /// <returns>The tails file async.</returns>
        /// <param name="tailsUri">Tails URI.</param>
        Task<byte[]> FetchTailsFileAsync(string tailsUri);
    }
}