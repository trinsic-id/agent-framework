using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Messages.Credentials;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Credential Service
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Gets credential record for the given identifier
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns>The stored credental record</returns>
        Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId);

        /// <summary>
        /// Retreives a list of <see cref="CredentialRecord"/> items for the given search criteria
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="query">The query.</param>
        /// <param name="count">The number of items to return</param>
        /// <returns>A list of credential records matchinc the search criteria</returns>
        Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null, int count = 100);

        /// <summary>
        /// Process the offer and stores in the desinated wallet asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialOffer">The credential offer.</param>
        /// <returns>The credential identifier of the stored credential record</returns>
        Task<string> ProcessOfferAsync(Wallet wallet, CredentialOfferMessage credentialOffer);

        /// <summary>
        /// Accepts the offer asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <param name="attributeValues">The attribute values.</param>
        /// <returns></returns>
        Task AcceptOfferAsync(Wallet wallet, Pool pool, string credentialId, Dictionary<string, string> attributeValues);

        /// <summary>
        /// Rejects a credential offer asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns></returns>
        Task RejectOfferAsync(Wallet wallet, string credentialId);

        /// <summary>
        /// Processes the issued credential and stores in the designated wallet.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credential">The credential.</param>
        /// <returns></returns>
        Task ProcessCredentialAsync(Pool pool, Wallet wallet, CredentialMessage credential);

        /// <summary>
        /// Create a new credential offer.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="config">A configuration object used to configure the resulting offers presentation</param>
        /// <returns>
        /// The offer.
        /// </returns>
        Task<CredentialOfferMessage> CreateOfferAsync(Wallet wallet, DefaultCreateOfferConfiguration config);

        /// <summary>
        /// Sends the offer asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="config">A configuration object used to configure the resulting offers presentation</param>
        /// <returns></returns>
        Task SendOfferAsync(Wallet wallet, DefaultCreateOfferConfiguration config);

        /// <summary>
        /// Processes the credential request and stores in the designated wallet.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialRequest">The credential request.</param>
        /// <returns>The credential identifier of the stored credential record.</returns>
        Task<string> ProcessCredentialRequestAsync(Wallet wallet, CredentialRequestMessage credentialRequest);

        /// <summary>
        /// Creates and sends a credential with the given credential identifier
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns></returns>
        Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId);

        /// <summary>
        /// Creates and sends a credential with the given credential identifier. 
        /// The credential is issued with the attributeValues provided.
        /// </summary>
        /// <returns>The credential async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="issuerDid">Issuer did.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="values">Values.</param>
        Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId, Dictionary<string, string> values);

        /// <summary>
        /// Rejects a credential request asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns></returns>
        Task RejectCredentialRequestAsync(Wallet wallet, string credentialId);

        /// <summary>
        /// Revokes an issued credentials and writes the updated revocation state to the ledger
        /// </summary>
        /// <returns>The credential async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="credentialId">Identifier of the credential to be revoked.</param>
        /// <param name="issuerDid">The DID of the issuer who issued the credential and owns the definitions</param>
        Task RevokeCredentialAsync(Pool pool, Wallet wallet, string credentialId, string issuerDid);
    }
}