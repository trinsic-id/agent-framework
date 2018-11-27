using System;
using System.Threading.Tasks;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Microsoft.Extensions.Caching.Memory;

namespace AgentFramework.AspNetCore.Runtime
{
    /// <inheritdoc />
    /// <summary>
    /// An implementation of <see cref="T:Streetcred.Sdk.Runtime.DefaultLedgerService" /> that uses <see cref="T:Microsoft.Extensions.Caching.Memory.IMemoryCache" />
    /// to store cached objects
    /// </summary>
    public class MemoryCacheLedgerService : DefaultLedgerService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _options;

        public MemoryCacheLedgerService(IMemoryCache memoryCache, MemoryCacheEntryOptions options = null)
        {
            _memoryCache = memoryCache;
            _options = options ?? new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1));
        }

        /// <inheritdoc />
        /// <summary>
        /// Looks up the schema details for the given <paramref name="schemaId" /> in the cache.
        /// If found, returns the cached value, otherwise performs a ledger lookup and caches the result.
        /// </summary>
        /// <returns>The schema async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="submitterDid">Submitter did.</param>
        /// <param name="schemaId">Schema identifier.</param>
        public override async Task<ParseResponseResult> LookupSchemaAsync(Pool pool, string submitterDid, string schemaId)
        {
            if (!_memoryCache.TryGetValue<ParseResponseResult>(schemaId, out var result))
            {
                result = await base.LookupSchemaAsync(pool, submitterDid, schemaId);
                // Save data in cache.
                _memoryCache.Set(schemaId, result, _options);
            }

            return result;
        }

        /// <inheritdoc />
        /// <summary>
        /// Looks up the credential definition for the given <paramref name="definitionId" /> in the cache.
        /// If found, returns the cached value, otherwise performs a ledger lookup and caches the result.
        /// </summary>
        /// <returns>The definition async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="submitterDid">Submitter did.</param>
        /// <param name="definitionId">Definition identifier.</param>
        public override async Task<ParseResponseResult> LookupDefinitionAsync(Pool pool, string submitterDid, string definitionId)
        {
            if (!_memoryCache.TryGetValue<ParseResponseResult>(definitionId, out var result))
            {
                result = await base.LookupDefinitionAsync(pool, submitterDid, definitionId);
                
                // Save data in cache.
                _memoryCache.Set(definitionId, result, _options);
            }

            return result;
        }

        /// <inheritdoc />
        /// <summary>
        /// Looks up the transaction for the given <paramref name="sequenceId" /> and <paramref name="ledgerType" /> combination in the cache.
        /// If found, returns the cached value, otherwise performs a ledger lookup and caches the result.
        /// </summary>
        /// <returns>The definition async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="submitterDid">Submitter did.</param>
        /// <param name="ledgerType">Ledger Type.</param>
        /// <param name="sequenceId">Sequence identifier.</param>
        public override async Task<string> LookupTransactionAsync(Pool pool, string submitterDid, string ledgerType, int sequenceId)
        {
            if (string.IsNullOrEmpty(ledgerType))
                ledgerType = "DOMAIN";

            if (!_memoryCache.TryGetValue<string>($"{ledgerType}-{sequenceId}", out var result))
            {
                result = await base.LookupTransactionAsync(pool, submitterDid, ledgerType, sequenceId);

                // Save data in cache.
                _memoryCache.Set($"{ledgerType}-{sequenceId}", result, _options);
            }

            return result;
        }
    }
}
