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
    /// A collection of convenience methods for the <see cref="ICredentialService"/> class.
    /// </summary>
    public static class CredentialServiceExtensions
    {
        /// <summary>
        /// Retrieves a list of credential offers
        /// </summary>
        /// <returns>The offers async.</returns>
        /// <param name="credentialService">Credential service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<CredentialRecord>> ListOffersAsync(this ICredentialService credentialService,
            Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, CredentialState.Offered.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of credential requests
        /// </summary>
        /// <returns>The requests async.</returns>
        /// <param name="credentialService">Credential service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<CredentialRecord>> ListRequestsAsync(this ICredentialService credentialService,
            Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, CredentialState.Requested.ToString("G")}}, count);

        /// <summary>
        /// Retreives a list of issued credentials
        /// </summary>
        /// <returns>The issued credentials async.</returns>
        /// <param name="credentialService">Credential service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<CredentialRecord>> ListIssuedCredentialsAsync(this ICredentialService credentialService,
            Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, CredentialState.Issued.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of revoked credentials
        /// </summary>
        /// <returns>The revoked credentials async.</returns>
        /// <param name="credentialService">Credential service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<CredentialRecord>> ListRevokedCredentialsAsync(
            this ICredentialService credentialService, Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, CredentialState.Revoked.ToString("G")}}, count);

        /// <summary>
        /// Retrieves a list of rejected/declined credentials.
        /// Rejected credentials will only be found in the issuer wallet, as the rejection is not communicated back to the holder.
        /// </summary>
        /// <returns>The rejected credentials async.</returns>
        /// <param name="credentialService">Credential service.</param>
        /// <param name="wallet">Wallet.</param>
        /// <param name="count">Count.</param>
        public static Task<List<CredentialRecord>> ListRejectedCredentialsAsync(
            this ICredentialService credentialService, Wallet wallet, int count = 100)
            => credentialService.ListAsync(wallet,
                new SearchRecordQuery {{ TagConstants.State, CredentialState.Rejected.ToString("G")}}, count);
    }
}