using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Pool service.
    /// </summary>
    public interface IDefaultPoolService
    {
        /// <summary>
        /// Opens the pool configuration with the specified name.
        /// </summary>
        /// <param name="poolName">Name of the pool configuration.</param>
        /// <param name="protocolVersion">The protocol version of the nodes.</param>
        /// <returns>
        /// A handle to the pool.
        /// </returns>
        Task<Pool> GetPoolAsync(string poolName, int protocolVersion);

        /// <summary>
        /// Creates a pool configuration.
        /// </summary>
        /// <param name="poolName">The name of the pool configuration.</param>
        /// <param name="genesisFile">Genesis transaction file.</param>
        /// <param name="protocolVersion">The protocol version of the nodes.</param>
        /// <returns>
        /// </returns>
        Task CreatePoolAsync(string poolName, string genesisFile, int protocolVersion);
    }
}
