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
    /// <inheritdoc />
    public class ABaseProvisioningService : IProvisioningService
    {
        protected readonly IWalletRecordService RecordService;

        public ABaseProvisioningService(IWalletRecordService walletRecord)
        {
            RecordService = walletRecord;
        }

        /// <inheritdoc />
        public virtual Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet) =>
            RecordService.GetAsync<ProvisioningRecord>(wallet, ProvisioningRecord.RecordId);

        /// <inheritdoc />
        public virtual async Task ProvisionAgentAsync(Wallet wallet, ProvisioningConfiguration provisioningConfiguration)
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

            await RecordService.AddAsync(wallet, record);
        }
    }
}