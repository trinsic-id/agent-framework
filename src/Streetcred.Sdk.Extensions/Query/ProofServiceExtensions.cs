using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Models.Records;
using Streetcred.Sdk.Models.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Extensions.Query
{
    /// <summary>
    /// A collection of convenience methods for the <see cref="IProofService"/> class.
    /// </summary>
    public static class ProofServiceExtensions
    {
        /// <summary>
        /// Retrieves a list of proof request records. 
        /// </summary>
        /// <param name="proofService">The proof service.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public static Task<List<ProofRecord>> ListRequestedAsync(this IProofService proofService,
            Wallet wallet, int count = 100)
            => proofService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, ProofState.Requested.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of accepted proof records.
        /// </summary>
        /// <param name="proofService">The proof service.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public static Task<List<ProofRecord>> ListAcceptedAsync(this IProofService proofService,
            Wallet wallet, int count = 100)
            => proofService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, ProofState.Accepted.ToString("G")}}, count);
    }
}
