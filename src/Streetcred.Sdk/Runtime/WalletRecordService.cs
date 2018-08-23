using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.NonSecretsApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Runtime
{
    /// <summary>
    /// Wallet record service.
    /// </summary>
    public class WalletRecordService : IWalletRecordService
    {
        /// <summary>
        /// Adds the record async.
        /// </summary>
        /// <returns>The record async.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="record">Record.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public Task AddAsync<T>(Wallet wallet, T record)
            where T : WalletRecord, new()
        {
            return NonSecrets.AddRecordAsync(wallet,
                record.GetTypeName(),
                record.GetId(),
                JsonConvert.SerializeObject(record),
                JsonConvert.SerializeObject(record.Tags));
        }

        /// <summary>
        /// Searchs the records async.
        /// </summary>
        /// <returns>The records async.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="query">Query.</param>
        /// <param name="options">Options.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<List<T>> SearchAsync<T>(Wallet wallet, SearchRecordQuery query, SearchRecordOptions options)
            where T : WalletRecord, new()
        {
            using (var search = await NonSecrets.OpenSearchAsync(wallet,
                new T().GetTypeName(),
                JsonConvert.SerializeObject(query ?? new SearchRecordQuery()),
                JsonConvert.SerializeObject(options ?? new SearchRecordOptions())))
            {
                var result = JsonConvert.DeserializeObject<SearchRecordResult>(await search.NextAsync(wallet, 10)); // TODO: Add support for pagination

                return result.Records?
                           .Select(x =>
                           {
                               var record = JsonConvert.DeserializeObject<T>(x.Value);
                               record.Tags = x.Tags;
                               return record;
                           })
                           .ToList()
                       ?? new List<T>();
            }
        }

        /// <summary>
        /// Updates the record async.
        /// </summary>
        /// <returns>The record async.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="record">Credential record.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task UpdateAsync<T>(Wallet wallet, T record) where T : WalletRecord, new()
        {
            await NonSecrets.UpdateRecordValueAsync(wallet,
                record.GetTypeName(),
                record.GetId(),
                JsonConvert.SerializeObject(record));

            await NonSecrets.UpdateRecordTagsAsync(wallet, 
                record.GetTypeName(), 
                record.GetId(),
                JsonConvert.SerializeObject(record.Tags));
        }

        /// <summary>
        /// Gets the record async.
        /// </summary>
        /// <returns>The record async.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="id">Identifier.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<T> GetAsync<T>(Wallet wallet, string id) where T : WalletRecord, new()
        {
            try
            {
                var recordJson = await NonSecrets.GetRecordAsync(wallet,
                    new T().GetTypeName(),
                    id,
                    JsonConvert.SerializeObject(new SearchRecordOptions()));

                if (recordJson == null) return null;

                var item = JsonConvert.DeserializeObject<SearchRecordItem>(recordJson);

                var record = JsonConvert.DeserializeObject<T>(item.Value);
                record.Tags = item.Tags;
                return record;
            }
            catch (WalletItemNotFoundException)
            {
                return null;
            }
        }
    }
}