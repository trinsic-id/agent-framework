using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ProofServiceExtensions
    {
        /// <summary>
        /// Retrieves a list of proof request records. 
        /// </summary>
        /// <param name="credentialService">The credential service.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public static Task<List<ProofRecord>> ListRequestedAsync(this IProofService credentialService,
            Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{"State", ProofState.Requested.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of accepted proof records.
        /// </summary>
        /// <param name="credentialService">The credential service.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public static Task<List<ProofRecord>> ListAcceptedAsync(this IProofService credentialService,
            Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{"State", ProofState.Accepted.ToString("G")}}, count);
    }
}
