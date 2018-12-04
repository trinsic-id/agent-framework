using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;

// ReSharper disable CheckNamespace

namespace Streetcred.Sdk.Contracts
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
                SearchQuery.Equal(nameof(ProofRecord.State), ProofState.Requested.ToString("G")), count);

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
                SearchQuery.Equal(nameof(ProofRecord.State), ProofState.Accepted.ToString("G")), count);
    }
}
