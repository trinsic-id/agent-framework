using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Credential service.
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Gets credential record for the given identifier
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns></returns>
        Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId);

        /// <summary>
        /// Lists the asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null);
    }
}
