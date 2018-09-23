using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;
using Multiformats.Base;
using Hyperledger.Indy.PoolApi;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.IO;

namespace Streetcred.Sdk.Runtime
{
    public class TailsService : ITailsService
    {
        private static readonly ConcurrentDictionary<string, BlobStorageReader> BlobReaders =
            new ConcurrentDictionary<string, BlobStorageReader>();

        private static readonly ConcurrentDictionary<string, BlobStorageWriter> BlobWriters =
            new ConcurrentDictionary<string, BlobStorageWriter>();

        private readonly ILedgerService _ledgerService;
        private readonly HttpClient _httpClient;

        public TailsService(ILedgerService ledgerService)
        {
            _ledgerService = ledgerService;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task<BlobStorageReader> GetTailsReaderAsync(string revocationRegistryId, Pool pool = null)
        {
            var baseDir = EnvironmentUtils.GetIndyHomePath("tails").Replace('\\', '/');
            var (attributeName, filename) = GetTailsAtribute(revocationRegistryId);

            var tailsWriterConfig = new
            {
                base_dir = baseDir,
                uri_pattern = string.Empty,
                file = filename
            };

            if (BlobReaders.TryGetValue(revocationRegistryId, out var blobReader))
            {
                return blobReader;
            }

            if (pool != null)
            {

                string targetDid = null;
                MessageUtils.FindFirstDid(revocationRegistryId, ref targetDid);
                var tailsUri = await _ledgerService.LookupAttributeAsync(pool, targetDid, attributeName);

                File.WriteAllBytes(path: Path.Combine(baseDir, filename),
                    bytes: await FetchTailsFileAsync(tailsUri));
            }

            blobReader = await BlobStorage.OpenReaderAsync("default", tailsWriterConfig.ToJson());

            BlobReaders.TryAdd(revocationRegistryId, blobReader);
            return blobReader;
        }

        //// <inheritdoc />
        public async Task<BlobStorageWriter> GetTailsWriterAsync(string revocationRegistryId)
        {
            var tailsWriterConfig = new
            {
                base_dir = EnvironmentUtils.GetIndyHomePath("tails").Replace('\\', '/'),
                uri_pattern = string.Empty,
                file = Multibase.Base58.Encode(Encoding.UTF8.GetBytes(revocationRegistryId))
            };

            if (BlobWriters.TryGetValue(revocationRegistryId, out var blobWriter))
            {
                return blobWriter;
            }

            blobWriter = await BlobStorage.OpenWriterAsync("default", tailsWriterConfig.ToJson());
            BlobWriters.TryAdd(revocationRegistryId, blobWriter);

            return blobWriter;
        }

        /// <inheritdoc />
        public Task<byte[]> FetchTailsFileAsync(string tailsUri) => _httpClient.GetByteArrayAsync(tailsUri);

        /// <summary>
        /// Gets the name of the tails location attribute on the ledger.
        /// </summary>
        /// <returns>The attribute name.</returns>
        /// <param name="revocationRegistryId">Revocation registry identifier.</param>
        public static (string attributeName, string tailsFilename) GetTailsAtribute(string revocationRegistryId) => (
            $"tails:location#{revocationRegistryId}",
            Multibase.Base58.Encode(Encoding.UTF8.GetBytes(revocationRegistryId)));
    }
}