using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Proof Service.
    /// </summary>
    public interface IProofService
    {
        /// <summary>
        /// Creates a proof request.
        /// </summary>
        /// <returns>The proof request.</returns>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="connectionId">Connection identifier of who the proof request will be sent to.</param>
        /// <param name="proofRequest">An enumeration of attribute we wish the prover to disclose.</param>
        /// <returns>Proof Request message and identifier.</returns>
        Task<(ProofRequestMessage, ProofRecord)> CreateProofRequestAsync(IAgentContext agentContext,
            string connectionId, ProofRequest proofRequest);

        /// <summary>
        /// Creates a proof request.
        /// </summary>
        /// <returns>The proof request.</returns>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="connectionId">Connection identifier of who the proof request will be sent to.</param>
        /// <param name="proofRequestJson">A string representation of proof request json object</param>
        /// <returns>Proof Request message and identifier.</returns>
        Task<(ProofRequestMessage, ProofRecord)> CreateProofRequestAsync(IAgentContext agentContext,
            string connectionId, string proofRequestJson);

        /// <summary>
        /// Processes a proof request and stores it for a given connection.
        /// </summary>
        /// <returns>The identifier for the stored proof request.</returns>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRequest">A proof request.</param>
        /// <returns>Proof identifier.</returns>
        Task<string> ProcessProofRequestAsync(IAgentContext agentContext, ProofRequestMessage proofRequest);

        /// <summary>
        /// Processes a proof and stores it for a given connection.
        /// </summary>
        /// <returns>The identifier for the stored proof.</returns>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proof">A proof.</param>
        /// <returns>Proof identifier.</returns>
        Task<string> ProcessProofAsync(IAgentContext agentContext, ProofMessage proof);

        /// <summary>
        /// Creates a proof.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRequestId">Identifier of the proof request.</param>
        /// <param name="requestedCredentials">The requested credentials.</param>
        /// <returns>
        /// The proof.
        /// </returns>
        Task<ProofMessage> CreateProofAsync(IAgentContext agentContext, string proofRequestId,
            RequestedCredentials requestedCredentials);

        /// <summary>
        /// Accepts a proof request by generating a proof and sending it to the requestor.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRequestId">Identifier of the proof request.</param>
        /// <param name="requestedCredentials">The requested credentials.</param>
        /// <returns>
        /// The proof.
        /// </returns>
        Task<ProofMessage> AcceptProofRequestAsync(IAgentContext agentContext, string proofRequestId,
            RequestedCredentials requestedCredentials);

        /// <summary>
        /// Rejects a proof request.
        /// </summary>
        /// <returns>The proof.</returns>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRequestId">Identifier of the proof request.</param>
        Task RejectProofRequestAsync(IAgentContext agentContext, string proofRequestId);

        /// <summary>
        /// Verifies a proof.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRecId">Identifier of the proof record.</param>
        /// <returns>Status indicating validity of proof</returns>
        Task<bool> VerifyProofAsync(IAgentContext agentContext, string proofRecId);

        /// <summary>
        /// Gets an enumeration of proofs stored in the wallet.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="query">The query.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// A list of proofs.
        /// </returns>
        Task<List<ProofRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100);

        /// <summary>
        /// Gets a particular proof stored in the wallet.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRecId">Identifier of the proof record.</param>
        /// <returns>The proof.</returns>
        Task<ProofRecord> GetAsync(IAgentContext agentContext, string proofRecId);

        /// <summary>
        /// Lists the credentials available for the given proof request.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="proofRequest">The proof request object.</param>
        /// <param name="attributeReferent">The attribute referent.</param>
        /// <returns>
        /// A collection of <see cref="CredentialInfo" /> that are available
        /// for building a proof for the given proof request
        /// </returns>
        Task<List<Credential>> ListCredentialsForProofRequestAsync(IAgentContext agentContext,
            ProofRequest proofRequest, string attributeReferent);
    }
}