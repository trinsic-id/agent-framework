using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models.Did;
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
        protected readonly IWalletService WalletService;

        public DefaultProvisioningService(
            IWalletRecordService walletRecord, 
            IWalletService walletService)
        {
            RecordService = walletRecord;
            WalletService = walletService;
        }

        /// <inheritdoc />
        public virtual async Task<ProvisioningRecord> GetProvisioningAsync(Wallet wallet)
        {
            var record = await RecordService.GetAsync<ProvisioningRecord>(wallet, ProvisioningRecord.UniqueRecordId);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Provisioning record not found");

            return record;
        }

        public async Task AddServiceAsync(Wallet wallet, IDidService service)
        {
            var record = await GetProvisioningAsync(wallet);

            record.Services.Add(service);

            await RecordService.UpdateAsync(wallet, record);
        }

        /// <inheritdoc />
        [Obsolete]
        public virtual async Task ProvisionAgentAsync(Wallet wallet, ProvisioningConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.WalletConfiguration == null ||
                configuration.WalletCredentials == null)
                throw new ArgumentNullException(nameof(configuration),
                    "Wallet configuration and credentials must be specified");

            ProvisioningRecord record = null;
            try
            {
                record = await GetProvisioningAsync(wallet);
            }
            catch (AgentFrameworkException e) when(e.ErrorCode == ErrorCode.RecordNotFound){}

            if (record != null)
                throw new AgentFrameworkException(ErrorCode.WalletAlreadyProvisioned);

            
            var masterSecretId = await AnonCreds.ProverCreateMasterSecretAsync(wallet, null);

            record = new ProvisioningRecord
            {
                MasterSecretId = masterSecretId,
                Owner = configuration.OwnershipInfo
            };

            if (configuration.IssuerAgentConfiguration != null)
            {
                var issuer = await Did.CreateAndStoreMyDidAsync(wallet,
                    configuration.IssuerAgentConfiguration.IssuerSeed != null
                        ? new {seed = configuration.IssuerAgentConfiguration.IssuerSeed}.ToJson()
                        : "{}");

                record.IssuerDid = issuer.Did;
                record.IssuerVerkey = issuer.VerKey;
                record.TailsBaseUri = configuration.IssuerAgentConfiguration.TailsBaseUri;
            }

            foreach (var service in configuration.AgentServices)
                record.Services.Add(service);

            await RecordService.AddAsync(wallet, record);
        }

        /// <inheritdoc />
        public async Task ProvisionAgentAsync(ProvisioningConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.WalletConfiguration == null ||
                configuration.WalletCredentials == null)
                throw new ArgumentNullException(nameof(configuration),
                    "Wallet configuration and credentials must be specified");

            try
            {
                // Create agent wallet
                await WalletService.CreateWalletAsync(configuration.WalletConfiguration, configuration.WalletCredentials);
            }
            catch (WalletExistsException)
            {
                throw new AgentFrameworkException(ErrorCode.WalletAlreadyProvisioned, "Wallet already provisioned");
            }

            var wallet =
                await WalletService.GetWalletAsync(configuration.WalletConfiguration, configuration.WalletCredentials);
            
            var masterSecretId = await AnonCreds.ProverCreateMasterSecretAsync(wallet, null);

            var record = new ProvisioningRecord
            {
                MasterSecretId = masterSecretId,
                Owner = configuration.OwnershipInfo
            };

            // Create issuer
            if (configuration.IssuerAgentConfiguration != null)
            {
                var issuer = await Did.CreateAndStoreMyDidAsync(wallet,
                    configuration.IssuerAgentConfiguration.IssuerSeed != null
                        ? new { seed = configuration.IssuerAgentConfiguration.IssuerSeed }.ToJson()
                        : "{}");

                record.IssuerDid = issuer.Did;
                record.IssuerVerkey = issuer.VerKey;
                record.TailsBaseUri = configuration.IssuerAgentConfiguration.TailsBaseUri;
            }

            foreach (var service in configuration.AgentServices)
                record.Services.Add(service);

            await RecordService.AddAsync(wallet, record);
        }

        /// <inheritdoc />
        public async Task UpdateServiceAsync(Wallet wallet, IDidService service)
        {
            var record = await GetProvisioningAsync(wallet);
            record.Services.Add(service);

            await RecordService.UpdateAsync(wallet, record);
        }
    }
}