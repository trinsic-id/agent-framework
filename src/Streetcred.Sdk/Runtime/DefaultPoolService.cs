using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultPoolService : IPoolService
    {
        protected static Pool Pool;

        /// <inheritdoc />
        public virtual async Task<Pool> GetPoolAsync(string poolName, int protocolVersion)
        {
            if (Pool != null) return Pool;

            await Pool.SetProtocolVersionAsync(protocolVersion);
            return Pool = await Pool.OpenPoolLedgerAsync(poolName, null);
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