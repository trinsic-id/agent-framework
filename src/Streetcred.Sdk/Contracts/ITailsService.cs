using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;

namespace Streetcred.Sdk.Contracts
{
    public interface ITailsService
    {

        /// <summary>
        /// Gets the BLOB storage reader async.
        /// </summary>
        /// <returns>The BLOB storage reader async.</returns>
        /// <param name="storageId">Storage identifier.</param>
        Task<BlobStorageReader> GetBlobStorageReaderAsync(string storageId);

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        /// <param name="storageId">Storage identifier.</param>
        Task<BlobStorageWriter> GetBlobStorageWriterAsync(string storageId);
    }
}
