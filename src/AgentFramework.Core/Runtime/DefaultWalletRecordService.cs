using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.NonSecretsApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultWalletRecordService : IWalletRecordService
    {
        /// <inheritdoc />
        public virtual Task AddAsync<T>(Wallet wallet, T record)
            where T : WalletRecord, new()
        {
            return NonSecrets.AddRecordAsync(wallet,
                record.GetTypeName(),
                record.GetId(),
                record.ToJson(),
                record.Tags.ToJson());
        }

        /// <inheritdoc />
        public virtual async Task<List<T>> SearchAsync<T>(Wallet wallet, ISearchQuery query, SearchOptions options, int count)
            where T : WalletRecord, new()
        {
            using (var search = await NonSecrets.OpenSearchAsync(wallet, new T().GetTypeName(),
                (query ?? SearchQuery.Empty).ToJson(),
                (options ?? new SearchOptions()).ToJson()))
            {
                var result = JsonConvert.DeserializeObject<SearchResult>(await search.NextAsync(wallet, count));
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
        public virtual async Task UpdateAsync<T>(Wallet wallet, T record) where T : WalletRecord, new()
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
        public virtual async Task<T> GetAsync<T>(Wallet wallet, string id) where T : WalletRecord, new()
        {
            try
            {
                var recordJson = await NonSecrets.GetRecordAsync(wallet,
                    new T().GetTypeName(),
                    id,
                    new SearchOptions().ToJson());

                if (recordJson == null) return null;

                var item = JsonConvert.DeserializeObject<SearchItem>(recordJson);

                var record = JsonConvert.DeserializeObject<T>(item.Value);
                record.Tags.Clear();
                foreach (var tag in item.Tags)
                    record.Tags[tag.Key] = tag.Value;
                return record;
            }
            catch (WalletItemNotFoundException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public virtual async Task<bool> DeleteAsync<T>(Wallet wallet, string id) where T : WalletRecord, new()
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