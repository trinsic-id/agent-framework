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
        public async Task<BlobStorageReader> OpenTailsAsync(string credentialDefinitionId, Pool pool = null)
        {
            var baseDir = EnvironmentUtils.GetTailsPath();
            var filename = credentialDefinitionId.ToBase58();

            var tailsWriterConfig = new
            {
                base_dir = baseDir,
                uri_pattern = string.Empty,
                file = filename
            };

            if (BlobReaders.TryGetValue(credentialDefinitionId, out var blobReader))
            {
                return blobReader;
            }

            if (pool != null)
            {
                await DownloadTailsAsync(pool, credentialDefinitionId, Path.Combine(baseDir, filename));
            }

            blobReader = await BlobStorage.OpenReaderAsync("default", tailsWriterConfig.ToJson());

            BlobReaders.TryAdd(credentialDefinitionId, blobReader);
            return blobReader;
        }

        //// <inheritdoc />
        public async Task<BlobStorageWriter> CreateTailsAsync(string credentialDefinitionId)
        {
            var tailsWriterConfig = new
            {
                base_dir = EnvironmentUtils.GetTailsPath(),
                uri_pattern = string.Empty,
                file = credentialDefinitionId.ToBase58()
            };

            if (BlobWriters.TryGetValue(credentialDefinitionId, out var blobWriter))
            {
                return blobWriter;
            }

            blobWriter = await BlobStorage.OpenWriterAsync("default", tailsWriterConfig.ToJson());
            BlobWriters.TryAdd(credentialDefinitionId, blobWriter);

            return blobWriter;
        }

        private async Task DownloadTailsAsync(Pool pool, string revocationRegistryId, string filename)
        {
            var revocationRegistry =
                await _ledgerService.LookupRevocationRegistryDefinitionAsync(pool, null, revocationRegistryId);
            var tailsUri = JObject.Parse(revocationRegistry.ObjectJson)["value"]["tailsLocation"].ToObject<string>();

            File.WriteAllBytes(path: filename,
                bytes: await _httpClient.GetByteArrayAsync(tailsUri));
        }
    }
}