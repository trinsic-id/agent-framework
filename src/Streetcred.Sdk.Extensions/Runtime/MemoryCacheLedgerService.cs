using System;
using System.Threading.Tasks;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Microsoft.Extensions.Caching.Memory;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Runtime;

namespace Streetcred.Sdk.Extensions.Runtime
{
    /// <summary>
    /// An immplementation of <see cref="DefaultLedgerService"/> that uses <see cref="IMemoryCache"/>
    /// to store cached objects
    /// </summary>
    public class MemoryCacheLedgerService : DefaultLedgerService
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheLedgerService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Looks up the schema details for the given <paramref name="schemaId"/> in the cache.
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

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1));

                // Save data in cache.
                _memoryCache.Set(schemaId, result, cacheEntryOptions);
            }

            return result;
        }

        /// <summary>
        /// Looks up the credential definition for the given <paramref name="definitionId"/> in the cache.
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
                result = await base.LookupSchemaAsync(pool, submitterDid, definitionId);

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1));

                // Save data in cache.
                _memoryCache.Set(definitionId, result, cacheEntryOptions);
            }

            return result;
        }
    }
}
