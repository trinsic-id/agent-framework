﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Credential Service
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Gets credential record for the given identifier.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The stored credental record</returns>
        Task<CredentialRecord> GetAsync(IAgentContext agentContext, string credentialId);

        /// <summary>
        /// Retreives a list of <see cref="CredentialRecord"/> items for the given search criteria.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="query">The query.</param>
        /// <param name="count">The number of items to return</param>
        /// <returns>A list of credential records matchinc the search criteria</returns>
        Task<List<CredentialRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100);

        /// <summary>
        /// Process the offer and stores in the desinated wallet asynchronous.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialOffer">The credential offer.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>The credential identifier of the stored credential record.</returns>
        Task<string> ProcessOfferAsync(IAgentContext agentContext, CredentialOfferMessage credentialOffer, ConnectionRecord connection);

        /// <summary>
        /// Accepts the offer asynchronous.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <param name="attributeValues">The attribute values.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordInInvalidState.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The response async.</returns>
        Task AcceptOfferAsync(IAgentContext agentContext, string credentialId, Dictionary<string, string> attributeValues = null);

        /// <summary>
        /// Rejects a credential offer asynchronous.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordInInvalidState.</exception>
        /// <returns>The response async.</returns>
        Task RejectOfferAsync(IAgentContext agentContext, string credentialId);

        /// <summary>
        /// Processes the issued credential and stores in the designated wallet.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="connection">The connection.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordInInvalidState.</exception>
        /// <returns>The identifier for the credential record.</returns>
        Task<string> ProcessCredentialAsync(IAgentContext agentContext, CredentialMessage credential, ConnectionRecord connection);

        /// <summary>
        /// Create a new credential offer.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="config">A configuration object used to configure the resulting offers presentation.</param>
        /// <param name="connectionId">The connection id.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The offer message and the identifer. </returns>
        Task<(CredentialOfferMessage, string)> CreateOfferAsync(IAgentContext agentContext, OfferConfiguration config,
            string connectionId = null);

        /// <summary>
        /// Revokes a credential offer.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="offerId">Id of the credential offer.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordInInvalidState.</exception>
        /// <returns>The response async.</returns>
        Task RevokeCredentialOfferAsync(IAgentContext agentContext, string offerId);

        /// <summary>
        /// Sends the offer asynchronous.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="connectionId">The connection id.</param>
        /// <param name="config">A configuration object used to configure the resulting offers presentation</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The offer identifier.</returns>
        Task<string> SendOfferAsync(IAgentContext agentContext, string connectionId, OfferConfiguration config);

        /// <summary>
        /// Processes the credential request and stores in the designated wallet.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialRequest">The credential request.</param>
        /// <param name="connection">The connection.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The credential identifier of the stored credential record.</returns>
        Task<string> ProcessCredentialRequestAsync(IAgentContext agentContext, CredentialRequestMessage credentialRequest, ConnectionRecord connection);

        /// <summary>
        /// Creates and sends a credential with the given credential identifier
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns>The response async.</returns>
        Task IssueCredentialAsync(IAgentContext agentContext, string issuerDid, string credentialId);

        /// <summary>
        /// Creates and sends a credential with the given credential identifier. 
        /// The credential is issued with the attributeValues provided.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="issuerDid">Issuer did.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="values">Values.</param>
        /// <returns>The response async.</returns>
        Task IssueCredentialAsync(IAgentContext agentContext, string issuerDid, string credentialId, Dictionary<string, string> values);

        /// <summary>
        /// Rejects a credential request asynchronous.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.RecordNotFound.</exception>
        /// <returns>The response async.</returns>
        Task RejectCredentialRequestAsync(IAgentContext agentContext, string credentialId);

        /// <summary>
        /// Revokes an issued credentials and writes the updated revocation state to the ledger
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="credentialId">Identifier of the credential to be revoked.</param>
        /// <param name="issuerDid">The DID of the issuer who issued the credential and owns the definitions</param>
        /// <returns>The response async.</returns>
        Task RevokeCredentialAsync(IAgentContext agentContext, string credentialId, string issuerDid);
    }
}