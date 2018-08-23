using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Sovrin.Agents.Model.Credentials;

namespace Streetcred.Sdk.Contracts
{
    public interface IAgencyCredentialService : ICredentialService
    {

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
        Task StoreCredentialRequest(Wallet wallet, CredentialRequest credentialRequest, string connectionId);

        /// <summary>
        /// Creates and sends a credential with the given credential identifier
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="credentialId">The credential identifier.</param>
        /// <returns></returns>
        Task IssueCredentialAsync(Pool pool, Wallet wallet, string issuerDid, string credentialId);
    }
}