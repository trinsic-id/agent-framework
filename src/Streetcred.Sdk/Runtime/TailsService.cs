using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    public class TailsService : ITailsService
    {
        private static readonly ConcurrentDictionary<string, BlobStorageReader> BlobReaders =
            new ConcurrentDictionary<string, BlobStorageReader>();

        private static readonly ConcurrentDictionary<string, BlobStorageWriter> BlobWriters =
            new ConcurrentDictionary<string, BlobStorageWriter>();
        /// <summary>
        /// Gets the BLOB storage reader async.
        /// </summary>
        /// <returns>The BLOB storage reader async.</returns>
        /// <param name="storageId">Storage identifier.</param>
        public async Task<BlobStorageReader> GetBlobStorageReaderAsync(string storageId)
        {
            var tailsWriterConfig =
                $"{{\"base_dir\":\"{EnvironmentUtils.GetIndyHomePath("tails", storageId)}\", \"uri_pattern\":\"\"}}"
                    .Replace('\\', '/');

            if (BlobReaders.TryGetValue(storageId, out var blobReader))
            {
                return blobReader;
            }

            blobReader = await BlobStorage.OpenReaderAsync("default", tailsWriterConfig);

            BlobReaders.TryAdd(storageId, blobReader);

            return blobReader;
        }

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        /// <param name="storageId">Storage identifier.</param>
        public async Task<BlobStorageWriter> GetBlobStorageWriterAsync(string storageId)
        {
            var tailsWriterConfig =
                $"{{\"base_dir\":\"{EnvironmentUtils.GetIndyHomePath("tails", storageId)}\", \"uri_pattern\":\"\"}}"
                    .Replace('\\', '/');

            if (BlobWriters.TryGetValue(storageId, out var blobWriter))
            {
                return blobWriter;
            }

            blobWriter = await BlobStorage.OpenWriterAsync("default", tailsWriterConfig);
            BlobWriters.TryAdd(storageId, blobWriter);

            return blobWriter;
        }
    }
}
