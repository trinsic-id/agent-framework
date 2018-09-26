using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace Streetcred.Sdk.Contracts
{
    public interface ITailsService
    {

        /// <summary>
        /// Opens an existing tails file and returns a handle.
        /// If <paramref name="pool"/> is specified, retreives the latest public tails file
        /// for the specified <paramref name="credentialDefinitionId"/> and stores it locally.
        /// </summary>
        /// <returns>The tails reader async.</returns>
        /// <param name="credentialDefinitionId">Credential definition identifier.</param>
        /// <param name="pool">Pool.</param>
        Task<BlobStorageReader> OpenTailsAsync(string credentialDefinitionId, Pool pool = null);

        /// <summary>
        /// Gets the BLOB storage writer async.
        /// </summary>
        /// <returns>The BLOB storage writer async.</returns>
        /// <param name="credentialDefinitionId">Credential definition identifier.</param>
        Task<BlobStorageWriter> CreateTailsAsync(string credentialDefinitionId);
    }
}