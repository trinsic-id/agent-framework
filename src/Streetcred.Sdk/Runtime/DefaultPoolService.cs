using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultPoolService : IPoolService
    {
        protected static readonly ConcurrentDictionary<string, Pool> Pools =
            new ConcurrentDictionary<string, Pool>();

        /// <inheritdoc />
        public virtual async Task<Pool> GetPoolAsync(string poolName, int protocolVersion)
        {
            if (Pools.TryGetValue(poolName, out var pool))
            {
                await Pool.SetProtocolVersionAsync(protocolVersion);
                return pool;
            }

            await Pool.SetProtocolVersionAsync(protocolVersion);
            pool = await Pool.OpenPoolLedgerAsync(poolName, null);

            Pools.TryAdd(poolName, pool);
            return pool;
        }

        /// <inheritdoc />
        public virtual async Task CreatePoolAsync(string poolName, string genesisFile, int protocolVersion)
        {
            await Pool.SetProtocolVersionAsync(protocolVersion);

            var poolConfig = new {genesis_txn = genesisFile}.ToJson();

            await Pool.CreatePoolLedgerConfigAsync(poolName, poolConfig);
        }
    }
}