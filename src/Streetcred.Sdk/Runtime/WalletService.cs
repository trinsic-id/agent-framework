using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Runtime
{
    public class WalletService : IWalletService
    {
        private static readonly ConcurrentDictionary<string, Wallet> Wallets =
            new ConcurrentDictionary<string, Wallet>();

        public const string MasterSecretName = "master_secret";

        /// <summary>
        /// Gets the wallet async.
        /// </summary>
        /// <returns>The wallet async.</returns>
        /// <param name="configuration">Configuration.</param>
        /// <param name="credentials">Credentials.</param>
        public async Task<Wallet> GetWalletAsync(WalletConfiguration configuration, WalletCredentials credentials)
        {

            if (Wallets.TryGetValue(configuration.Id, out var wallet))
            {
                return wallet;
            }

            wallet = await Wallet.OpenWalletAsync(JsonConvert.SerializeObject(configuration),
                JsonConvert.SerializeObject(credentials));

            Wallets.TryAdd(configuration.Id, wallet);

            return wallet;
        }

        /// <summary>
        /// Creates the wallet async.
        /// </summary>
        /// <returns>The wallet async.</returns>
        /// <param name="configuration">Configuration.</param>
        /// <param name="credentials">Credentials.</param>
        public async Task CreateWalletAsync(WalletConfiguration configuration, WalletCredentials credentials)
        {
            await Wallet.CreateWalletAsync(JsonConvert.SerializeObject(configuration),
                JsonConvert.SerializeObject(credentials));

            // Create master secret. This should later be moved to a credential related context
            await AnonCreds.ProverCreateMasterSecretAsync(await GetWalletAsync(configuration, credentials),
                MasterSecretName);
        }
    }
}