using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Contracts
{
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
        /// Stores the offer asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialOffer">The credential offer.</param>
        /// <returns>The credential identifier of the stored credential record</returns>
        Task<string> StoreOfferAsync(Wallet wallet, CredentialOffer credentialOffer);

        /// <summary>
        /// Accepts the offer asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        Task AcceptOfferAsync(Wallet wallet, Pool pool, string credentialId, Dictionary<string, string> values);


        /// <summary>
        /// Stores the issued credential in the designated wallet.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credential">The credential.</param>
        /// <returns></returns>
        Task StoreCredentialAsync(Pool pool, Wallet wallet, Credential credential);


        /// <summary>
        /// Sends the offer.
        /// </summary>
        /// <param name="credentialDefinitionId">Cred def identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <returns>
        /// The offer.
        /// </returns>
        Task<CredentialOffer> CreateOfferAsync(string credentialDefinitionId, string connectionId, Wallet wallet, string issuerDid);

        /// <summary>
        /// Sends the offer asynchronous.
        /// </summary>
        /// <param name="credentialDefinitionId">The credential definition identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <returns></returns>
        Task SendOfferAsync(string credentialDefinitionId, string connectionId, Wallet wallet, string issuerDid);

        /// <summary>
        /// Stores the credential request.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialRequest">The credential request.</param>
        /// <returns>The credential identifier of the stored credential record.</returns>
        Task<string> StoreCredentialRequestAsync(Wallet wallet, CredentialRequest credentialRequest);

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
        /// The credential is issued with the values provided.
        /// </summary>
        /// <returns>The credential async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="issuerDid">Issuer did.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="values">Values.</param>
        Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId, Dictionary<string, string> values);

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