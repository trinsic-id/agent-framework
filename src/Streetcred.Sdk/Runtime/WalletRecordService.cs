using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.NonSecretsApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class WalletRecordService : IWalletRecordService
    {
        /// <inheritdoc />
        public Task AddAsync<T>(Wallet wallet, T record)
            where T : WalletRecord, new()
        {
            return NonSecrets.AddRecordAsync(wallet,
                record.GetTypeName(),
                record.GetId(),
                record.ToJson(),
                record.Tags.ToJson());
        }

        /// <inheritdoc />
        public async Task<List<T>> SearchAsync<T>(Wallet wallet, SearchRecordQuery query, SearchRecordOptions options, int count)
            where T : WalletRecord, new()
        {
            using (var search = await NonSecrets.OpenSearchAsync(wallet, new T().GetTypeName(),
                (query ?? new SearchRecordQuery()).ToJson(),
                (options ?? new SearchRecordOptions()).ToJson()))
            {
                var result = JsonConvert.DeserializeObject<SearchRecordResult>(await search.NextAsync(wallet, count));
                // TODO: Add support for pagination

                return result.Records?
                           .Select(x =>
                           {
                               var record = JsonConvert.DeserializeObject<T>(x.Value);
                               record.Tags.Clear();
                               foreach (var tag in x.Tags)
                                   record.Tags.Add(tag.Key, tag.Value);
                               return record;
                           })
                           .ToList()
                       ?? new List<T>();
            }
        }

        /// <inheritdoc />
        public async Task UpdateAsync<T>(Wallet wallet, T record) where T : WalletRecord, new()
        {
            await NonSecrets.UpdateRecordValueAsync(wallet,
                record.GetTypeName(),
                record.GetId(),
                record.ToJson());

            await NonSecrets.UpdateRecordTagsAsync(wallet,
                record.GetTypeName(),
                record.GetId(),
                record.Tags.ToJson());
        }

        /// <inheritdoc />
        public async Task<T> GetAsync<T>(Wallet wallet, string id) where T : WalletRecord, new()
        {
            try
            {
                var recordJson = await NonSecrets.GetRecordAsync(wallet,
                    new T().GetTypeName(),
                    id,
                    new SearchRecordOptions().ToJson());

                if (recordJson == null) return null;

                var item = JsonConvert.DeserializeObject<SearchRecordItem>(recordJson);

                var record = JsonConvert.DeserializeObject<T>(item.Value);
                record.Tags.Clear();
                foreach (var tag in item.Tags)
                    record.Tags.Add(tag.Key, tag.Value);
                return record;
            }
            catch (WalletItemNotFoundException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync<T>(Wallet wallet, string id) where T : WalletRecord, new()
        {
            try
            {
                await NonSecrets.DeleteRecordAsync(wallet,
                     new T().GetTypeName(),
                     id);

                return true;
            }
            catch (WalletItemNotFoundException)
            {
                return false;
            }
        }
    }
}