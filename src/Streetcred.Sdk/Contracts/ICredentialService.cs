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
        /// <returns></returns>
        Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId);

        /// <summary>
        /// Lists the asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null);

        /// <summary>
        /// Stores the offer asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialOffer">The credential offer.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns>The credential identifier of the stored record</returns>
        Task<string> StoreOfferAsync(Wallet wallet, CredentialOffer credentialOffer, string connectionId);

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
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns></returns>
        Task StoreCredentialAsync(Pool pool, Wallet wallet, Credential credential, string connectionId);


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
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns></returns>
        Task<string> StoreCredentialRequestAsync(Wallet wallet, CredentialRequest credentialRequest, string connectionId);

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