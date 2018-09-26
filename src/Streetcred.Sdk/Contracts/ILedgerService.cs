using System.Threading.Tasks;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Ledger service.
    /// </summary>
    public interface ILedgerService
    {
        /// <summary>
        /// Looks up an attribute value on the ledger.
        /// </summary>
        /// <returns>The attribute value or <c>null</c> if none were found.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="targetDid">The target DID for the <paramref name="attributeName"/> lookup</param>
        /// <param name="attributeName">Attribute name.</param>
        Task<string> LookupAttributeAsync(Pool pool, string targetDid, string attributeName);

        /// <summary>
        /// Register an attribute for the specified <paramref name="targetDid"/> to the ledger.
        /// </summary>
        /// <returns>The attribute async.</returns>
        /// <param name="pool">Pool.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="submittedDid">Submitted did.</param>
        /// <param name="targetDid">Target did.</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <param name="value">The attribute value.</param>
        Task RegisterAttributeAsync(Pool pool, Wallet wallet, string submittedDid, string targetDid,
            string attributeName, object value);

        /// <summary>
        /// Lookups the schema async.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="submitterDid">The submitter did.</param>
        /// <param name="schemaId">Schema identifier.</param>
        /// <returns>
        /// The schema async.
        /// </returns>
        Task<ParseResponseResult> LookupSchemaAsync(Pool pool, string submitterDid, string schemaId);

        /// <summary>
        /// Lookups the definition async.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="submitterDid">The submitter did.</param>
        /// <param name="definitionId">Definition identifier.</param>
        /// <returns>
        /// The definition async.
        /// </returns>
        Task<ParseResponseResult> LookupDefinitionAsync(Pool pool, string submitterDid, string definitionId);

        /// <summary>
        /// Lookups the revocation registry defintion.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="submitterDid">The submitter did.</param>
        /// <param name="registryId">The registry identifier.</param>
        /// <returns></returns>
        Task<ParseResponseResult> LookupRevocationRegistryDefinitionAsync(Pool pool, string submitterDid,
            string registryId);

        /// <summary>
        /// Registers the trust anchor async.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <param name="submitterDid">The submitter did.</param>
        /// <param name="theirDid">Their did.</param>
        /// <param name="theirVerkey">Their verkey.</param>
        /// <returns>
        /// The trust anchor async.
        /// </returns>
        Task RegisterTrustAnchorAsync(Wallet wallet, Pool pool, string submitterDid, string theirDid,
            string theirVerkey);

        /// <summary>
        /// Registers the credential definition async.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <param name="submitterDid">The submitter did.</param>
        /// <param name="data">Data.</param>
        /// <returns>
        /// The credential definition async.
        /// </returns>
        Task RegisterCredentialDefinitionAsync(Wallet wallet, Pool pool, string submitterDid, string data);

        /// <summary>
        /// Registers the revocation registry definition asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <param name="submitterDid">The submitter did.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        Task RegisterRevocationRegistryDefinitionAsync(Wallet wallet, Pool pool, string submitterDid, string data);

        /// <summary>
        /// Sends the revocation registry entry asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="revocationRegistryDefinitionId">The revocation registry definition identifier.</param>
        /// <param name="revocationDefinitionType">Type of the revocation definition.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        Task SendRevocationRegistryEntryAsync(Wallet wallet, Pool pool, string issuerDid,
            string revocationRegistryDefinitionId, string revocationDefinitionType, string value);

        /// <summary>
        /// Registers the schema asynchronous.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="schemaJson">The schema json.</param>
        /// <returns></returns>
        Task RegisterSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string schemaJson);
    }
}