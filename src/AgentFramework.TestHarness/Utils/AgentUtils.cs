using System;
using System.Threading.Tasks;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.TestHarness.Utils
{
    public class AgentUtils
    {
        public static async Task<AgentContext> Create(string config, string credentials, bool withPool = false)
        {
            try
            {
                await Wallet.CreateWalletAsync(config, credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            return new AgentContext { Wallet = await Wallet.OpenWalletAsync(config, credentials), Pool = withPool ? new PoolAwaitable(PoolUtils.GetPoolAsync) : PoolAwaitable.FromPool(null) };
        }

        public static Task<AgentContext> CreateRandomAgent(bool withPool= false)
        {
            return Create($"{{\"id\":\"{Guid.NewGuid()}\"}}", "{\"key\":\"test_wallet_key\"}", withPool);
        }
    }
}
