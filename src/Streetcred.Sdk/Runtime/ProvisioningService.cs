using System;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Exceptions;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc cref="IProvisioningService"/>
    public class ProvisioningService : IProvisioningService
    {
        private readonly IWalletRecordService _recordService;

        public ProvisioningService(IWalletRecordService walletRecord)
        {
            this._recordService = walletRecord;
        }


        /// <inheritdoc cref="IProvisioningService.GetProvisioningAsync"/>
        public Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet) =>
            _recordService.GetAsync<ProvisioningRecord>(wallet, ProvisioningRecord.RecordId);

        /// <inheritdoc cref="IProvisioningService.ProvisionAgentAsync"/>
        /// <exception cref="System.ArgumentNullException">
        /// provisioningRequest
        /// or
        /// EndpointUri
        /// </exception>
        /// <exception cref="StreetcredSdkException">Wallet is already provisioned.</exception>
        public async Task ProvisionAgentAsync(Wallet wallet, ProvisioningRequest provisioningRequest)
        {
            if (provisioningRequest == null) throw new ArgumentNullException(nameof(provisioningRequest));
            if (provisioningRequest.EndpointUri == null)
                throw new ArgumentNullException(nameof(provisioningRequest.EndpointUri));

            var record = await GetProvisioningAsync(wallet);
            if (record != null) throw new StreetcredSdkException("Wallet is already provisioned.");

            var agent = await Did.CreateAndStoreMyDidAsync(wallet,
                provisioningRequest.AgentSeed == null
                    ? new {seed = provisioningRequest.AgentSeed}.ToJson()
                    : "{}");

            record = new ProvisioningRecord
            {
                IssuerSeed = provisioningRequest.IssuerSeed,
                AgentSeed = provisioningRequest.AgentSeed,
                Endpoint =
                {
                    Uri = provisioningRequest.EndpointUri.ToString(),
                    Did = agent.Did,
                    Verkey = agent.VerKey
                }
            };

            if (provisioningRequest.CreateIssuer)
            {
                var issuer = await Did.CreateAndStoreMyDidAsync(wallet,
                    provisioningRequest.IssuerSeed == null
                        ? new {seed = provisioningRequest.IssuerSeed}.ToJson()
                        : "{}");

                record.IssuerDid = issuer.Did;
                record.IssuerVerkey = issuer.VerKey;
            }

            await _recordService.AddAsync(wallet, record);
        }
    }
}