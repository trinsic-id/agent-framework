using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <summary>
    /// Pool service.
    /// </summary>
    public class PoolService : IPoolService
    {
        private static Pool _pool;

        /// <inheritdoc />
        public async Task<Pool> GetPoolAsync(string poolName, int protocolVersion)
        {
            if (_pool != null) return _pool;

            await Pool.SetProtocolVersionAsync(protocolVersion);
            return _pool = await Pool.OpenPoolLedgerAsync(poolName, null);
        }

        /// <inheritdoc />
        public async Task CreatePoolAsync(string poolName, string genesisFile, int protocolVersion)
        {
            await Pool.SetProtocolVersionAsync(protocolVersion);

            var poolConfig = new {genesis_txn = genesisFile}.ToJson();

            await Pool.CreatePoolLedgerConfigAsync(poolName, poolConfig);
        }
    }
}