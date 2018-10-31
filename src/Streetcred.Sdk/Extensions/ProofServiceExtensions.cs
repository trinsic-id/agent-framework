using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Extensions
{
    /// <summary>
    /// A collection of convenience methods for the <see cref="IDefaultProofService"/> class.
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
        public static Task<List<ProofRecord>> ListRequestedAsync(this IDefaultProofService proofService,
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
        public static Task<List<ProofRecord>> ListAcceptedAsync(this IDefaultProofService proofService,
            Wallet wallet, int count = 100)
            => proofService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, ProofState.Accepted.ToString("G")}}, count);
    }
}
