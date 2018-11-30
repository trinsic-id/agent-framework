using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultProvisioningService : IProvisioningService
    {
        protected readonly IWalletRecordService RecordService;

        public DefaultProvisioningService(IWalletRecordService walletRecord)
        {
            RecordService = walletRecord;
        }

        /// <inheritdoc />
        public virtual async Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet)
        {
            var record = await RecordService.GetAsync<ProvisioningRecord>(wallet, ProvisioningRecord.UniqueRecordId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Provisioning record not found");

            return record;
        }

        /// <inheritdoc />
        public virtual async Task ProvisionAgentAsync(Wallet wallet, ProvisioningConfiguration provisioningConfiguration)
        {
            if (provisioningConfiguration == null) throw new ArgumentNullException(nameof(provisioningConfiguration));
            if (provisioningConfiguration.EndpointUri == null)
                throw new ArgumentNullException(nameof(provisioningConfiguration.EndpointUri));

            ProvisioningRecord record = null;
            try
            {
                record = await GetProvisioningAsync(wallet);
            }
            catch (AgentFrameworkException e) when(e.ErrorCode == ErrorCode.RecordNotFound){}

            if (record != null)
                throw new AgentFrameworkException(ErrorCode.WalletAlreadyProvisioned);

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
                TailsBaseUri = provisioningConfiguration.TailsBaseUri.ToString()
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