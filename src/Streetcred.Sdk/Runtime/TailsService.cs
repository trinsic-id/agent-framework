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
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;

namespace Streetcred.Sdk.Runtime
{
    public class TailsService : ITailsService
    {
        private static readonly ConcurrentDictionary<string, BlobStorageReader> BlobReaders =
            new ConcurrentDictionary<string, BlobStorageReader>();

        private static readonly ConcurrentDictionary<string, BlobStorageWriter> BlobWriters =
            new ConcurrentDictionary<string, BlobStorageWriter>();

        private readonly ILedgerService _ledgerService;
        private readonly IProvisioningService _provisioningService;
        private readonly HttpClient _httpClient;

        public TailsService(ILedgerService ledgerService,
            IProvisioningService provisioningService)
        {
            _ledgerService = ledgerService;
            _provisioningService = provisioningService;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task<BlobStorageReader> OpenTailsAsync(string filename)
        {
            var baseDir = EnvironmentUtils.GetTailsPath();

            var tailsWriterConfig = new
            {
                base_dir = baseDir,
                uri_pattern = string.Empty,
                file = filename
            };

            if (BlobReaders.TryGetValue(filename, out var blobReader))
            {
                return blobReader;
            }

            blobReader = await BlobStorage.OpenReaderAsync("default", tailsWriterConfig.ToJson());
            BlobReaders.TryAdd(filename, blobReader);
            return blobReader;
        }

        /// <inheritdoc />
        public async Task<BlobStorageWriter> CreateTailsAsync()
        {
            var tailsWriterConfig = new
            {
                base_dir = EnvironmentUtils.GetTailsPath(),
                uri_pattern = string.Empty
            };

            var blobWriter = await BlobStorage.OpenWriterAsync("default", tailsWriterConfig.ToJson());
            return blobWriter;
        }

        /// <inheritdoc />
        public async Task EnsureTailsExistsAsync(Pool pool, string revocationRegistryId)
        {
            var revocationRegistry =
                await _ledgerService.LookupRevocationRegistryDefinitionAsync(pool, null, revocationRegistryId);
            var tailsUri = JObject.Parse(revocationRegistry.ObjectJson)["value"]["tailsLocation"].ToObject<string>();

            var filename = new Uri(tailsUri).Segments.Last();


            File.WriteAllBytes(
                path: filename,
                bytes: await _httpClient.GetByteArrayAsync(tailsUri));
        }
    }
}