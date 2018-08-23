using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model.Records;

namespace Streetcred.Sdk.Contracts
{
    public interface ISchemaService
    {
        /// <summary>
        /// Creates the schema asynchronous.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        /// <param name="attributeNames">The attribute names.</param>
        /// <returns></returns>
        Task<string> CreateSchemaAsync(Pool pool, Wallet wallet, string issuerDid, string name, string version,
            string[] attributeNames);

        /// <summary>
        /// Creates the credential definition asynchronous.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="schemaId">The schema identifier.</param>
        /// <param name="issuerDid">The issuer did.</param>
        /// <param name="supportsRevocation">if set to <c>true</c> [supports revocation].</param>
        /// <returns></returns>
        Task<string> CreateCredentialDefinitionAsync(Pool pool, Wallet wallet, string schemaId, string issuerDid,
            bool supportsRevocation);

        /// <summary>
        /// Gets the schemas asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <returns></returns>
        Task<List<SchemaRecord>> ListSchemasAsync(Wallet wallet);

        /// <summary>
        /// Gets the credential definitions asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <returns></returns>
        Task<List<DefinitionRecord>> ListCredentialDefinitionsAsync(Wallet wallet);

        /// <summary>
        /// Gets the credential definition asynchronous.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="credentialDefinitionId">The credential definition identifier.</param>
        /// <returns></returns>
        Task<DefinitionRecord> GetCredentialDefinitionAsync(Wallet wallet, string credentialDefinitionId);
    }
}
