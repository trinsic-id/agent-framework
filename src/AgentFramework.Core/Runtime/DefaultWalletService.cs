using System.Collections.Concurrent;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultWalletService : IWalletService
    {
        protected static readonly ConcurrentDictionary<string, Wallet> Wallets =
            new ConcurrentDictionary<string, Wallet>();

        /// <inheritdoc />
        public virtual async Task<Wallet> GetWalletAsync(WalletConfiguration configuration, WalletCredentials credentials)
        {
            if (Wallets.TryGetValue(configuration.Id, out var wallet))
                return wallet;

            wallet = await Wallet.OpenWalletAsync(configuration.ToJson(), credentials.ToJson());

            Wallets.TryAdd(configuration.Id, wallet);

            return wallet;
        }

        /// <inheritdoc />
        public virtual async Task CreateWalletAsync(WalletConfiguration configuration, WalletCredentials credentials)
        {
            await Wallet.CreateWalletAsync(configuration.ToJson(), credentials.ToJson());
        }

        /// <inheritdoc />
        public virtual async Task DeleteWalletAsync(WalletConfiguration configuration, WalletCredentials credentials)
        {
            await Wallet.DeleteWalletAsync(configuration.ToJson(), credentials.ToJson());
        }
    }
}