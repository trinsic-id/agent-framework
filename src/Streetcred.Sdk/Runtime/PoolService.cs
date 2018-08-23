using System.Diagnostics;
using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;

namespace Streetcred.Sdk.Runtime
{
    /// <summary>
    /// Pool service.
    /// </summary>
    public class PoolService : IPoolService
    {
        private static Pool _pool;

        /// <inheritdoc />
        public async Task<Pool> GetPoolAsync(string poolName = "DefaultPool")
        {
            if (_pool != null) return _pool;

            await Pool.SetProtocolVersionAsync(2);
            return _pool = await Pool.OpenPoolLedgerAsync(poolName, null);
        }

        /// <summary>
        /// Creates the pool async.
        /// </summary>
        /// <returns>The pool async.</returns>
        public async Task CreatePoolAsync(string poolName, string genesisFile)
        {
            try
            {
                await Pool.SetProtocolVersionAsync(2);

                var poolConfig = JsonConvert.SerializeObject(new { genesis_txn = genesisFile });

                await Pool.CreatePoolLedgerConfigAsync(poolName, poolConfig);
            }
            catch (PoolLedgerConfigExistsException)
            {
                Debug.WriteLine("Pool configuration exists");
            }
        }
    }
}
