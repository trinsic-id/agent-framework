using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Pool service.
    /// </summary>
    public interface IPoolService
    {
        /// <summary>
        /// Gets the pool async.
        /// </summary>
        /// <returns>The pool async.</returns>
        Task<Pool> GetPoolAsync(string poolName = "DefaultPool");

        /// <summary>
        /// Creates the pool async.
        /// </summary>
        /// <returns>The pool async.</returns>
        /// <param name="poolName">Pool name.</param>
        /// <param name="genesisFile">Genesis file.</param>
        Task CreatePoolAsync(string poolName, string genesisFile);
    }
}
