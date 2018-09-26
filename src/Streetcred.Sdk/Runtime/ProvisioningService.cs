using System;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Exceptions;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Wallets;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc cref="IProvisioningService"/>
    public class ProvisioningService : IProvisioningService
    {
        private readonly IWalletRecordService _recordService;

        public ProvisioningService(IWalletRecordService walletRecord)
        {
            _recordService = walletRecord;
        }

        /// <inheritdoc cref="IProvisioningService.GetProvisioningAsync"/>
        public Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet) =>
            _recordService.GetAsync<ProvisioningRecord>(wallet, ProvisioningRecord.RecordId);

        /// <inheritdoc cref="IProvisioningService.ProvisionAgentAsync"/>
        /// <exception cref="System.ArgumentNullException">
        /// provisioningConfiguration
        /// or
        /// EndpointUri
        /// </exception>
        /// <exception cref="WalletAlreadyProvisionedException">Wallet is already provisioned.</exception>
        public async Task ProvisionAgentAsync(Wallet wallet, ProvisioningConfiguration provisioningConfiguration)
        {
            if (provisioningConfiguration == null) throw new ArgumentNullException(nameof(provisioningConfiguration));
            if (provisioningConfiguration.EndpointUri == null)
                throw new ArgumentNullException(nameof(provisioningConfiguration.EndpointUri));

            var record = await GetProvisioningAsync(wallet);
            if (record != null) throw new WalletAlreadyProvisionedException();

            var agent = await Did.CreateAndStoreMyDidAsync(wallet,
                provisioningConfiguration.AgentSeed != null
                    ? new {seed = provisioningConfiguration.AgentSeed}.ToJson()
                    : "{}");

            var masterSecretId = await AnonCreds.ProverCreateMasterSecretAsync(wallet, null);

            record = new ProvisioningRecord
            {
                IssuerSeed = provisioningConfiguration.IssuerSeed,
                AgentSeed = provisioningConfiguration.AgentSeed,
                MasterSecretId = masterSecretId,
                Endpoint =
                {
                    Uri = provisioningConfiguration.EndpointUri.ToString(),
                    Did = agent.Did,
                    Verkey = agent.VerKey
                },
                Owner =
                {
                    Name = provisioningConfiguration.OwnerName,
                    ImageUrl = provisioningConfiguration.OwnerImageUrl
                },
                TailsBaseUri = provisioningConfiguration.TailsBaseUri
            };

            if (provisioningConfiguration.CreateIssuer)
            {
                var issuer = await Did.CreateAndStoreMyDidAsync(wallet,
                    provisioningConfiguration.IssuerSeed != null
                        ? new {seed = provisioningConfiguration.IssuerSeed}.ToJson()
                        : "{}");

                record.IssuerDid = issuer.Did;
                record.IssuerVerkey = issuer.VerKey;
            }

            await _recordService.AddAsync(wallet, record);
        }
    }
}