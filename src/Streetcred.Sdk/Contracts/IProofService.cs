using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model.Proofs;

namespace Streetcred.Sdk.Contracts
{
    public interface IProofService
    {
        /// <summary>
        /// Sends a proof request
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="connectionId">Connection identifier of who the proof request will be sent to.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="attributes">An enumeration of attribute we wish the prover to disclose.</param>
        Task SendProofRequestAsync(string connectionId, Wallet wallet, IEnumerable<string> attributes);

        /// <summary>
        /// Sends a proof request
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="connectionId">Connection identifier of who the proof request will be sent to.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRequestJson">A string representation of proof request json object.</param>
        Task SendProofRequestAsync(string connectionId, Wallet wallet, string proofRequestJson);

        /// <summary>
        /// Creates a proof request
        /// </summary>
        /// <returns>The proof request.</returns>
        /// <param name="connectionId">Connection identifier of who the proof request will be sent to.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="attributes">An enumeration of attribute we wish the prover to disclose.</param>
        Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet, IEnumerable<string> attributes);

        /// <summary>
        /// Creates a proof request
        /// </summary>
        /// <returns>The proof request.</returns>
        /// <param name="connectionId">Connection identifier of who the proof request will be sent to.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRequestJson">A string representation of proof request json object</param>
        Task<ProofRequest> CreateProofRequestAsync(string connectionId, Wallet wallet, string proofRequestJson);

        /// <summary>
        /// Stores a proof request
        /// </summary>
        /// <returns>The identifier for the stored proof request.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRequest">A proof request.</param>
        Task<string> StoreProofRequestAsync(Wallet wallet, ProofRequest proofRequest);

        /// <summary>
        /// Stores a proof
        /// </summary>
        /// <returns>The identifier for the stored proof.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proof">A proof.</param>
        Task<string> StoreProofAsync(Wallet wallet, Proof proof);

        /// <summary>
        /// Creates a proof
        /// </summary>
        /// <returns>The proof.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRequestId">Identifier of the proof request.</param>
        Task<Proof> CreateProof(Wallet wallet, string proofRequestId);

        /// <summary>
        /// Accepts a proof request by generating a proof and sending it to the requestor
        /// </summary>
        /// <returns>The proof.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRequestId">Identifier of the proof request.</param>
        Task AcceptProofRequestAsync(Wallet wallet, string proofRequestId);

        /// <summary>
        /// Rejects a proof request
        /// </summary>
        /// <returns>The proof.</returns>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRequestId">Identifier of the proof request.</param>
        Task RejectProofRequestAsync(Wallet wallet, string proofRequestId);

        /// <summary>
        /// Verifies a proof 
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRecId">Identifier of the proof record.</param>
        /// <returns></returns>
        Task<bool> VerifyProofAsync(Wallet wallet, string proofRecId);

        /// <summary>
        /// Gets an enumeration of proofs stored in the wallet
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <returns>A list of proofs.</returns>
        Task<IEnumerable<string>> GetProofs(Wallet wallet); //TODO this will have some search parameters

        /// <summary>
        /// Gets a particular proof stored in the wallet
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRecId">Identifier of the proof record.</param>
        /// <returns>The proof.</returns>
        Task<string> GetProof(Wallet wallet, string proofRecId);

        /// <summary>
        /// Gets an enumeration of proof requests stored in the wallet
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <returns>A list of proofs requests.</returns>
        Task<IEnumerable<string>> GetProofRequests(Wallet wallet); //TODO this will have some search parameters

        /// <summary>
        /// Gets a particular proof request stored in the wallet
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <param name="proofRecId">Identifier of the proof record.</param>
        /// <returns>The proof.</returns>
        Task<string> GetProofRequest(Wallet wallet, string proofRecId);

    }
}