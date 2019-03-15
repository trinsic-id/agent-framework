using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models;
using Microsoft.Extensions.Options;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultAgentContextProvider : IAgentContextProvider
    {
        private readonly WalletOptions _walletOptions;

        private readonly PoolOptions _poolOptions;

        private readonly IWalletService _walletService;

        private readonly IPoolService _poolService;

        /// <summary>
        /// Default Agent Context Provider.
        /// </summary>
        /// <param name="walletOptions">The wallet options provider.</param>
        /// <param name="poolOptions">The pool options provider/</param>
        /// <param name="walletService">The wallet service.</param>
        /// <param name="poolService">The pool service.</param>
        public DefaultAgentContextProvider(IOptions<WalletOptions> walletOptions,
                                          IOptions<PoolOptions> poolOptions,
                                          IWalletService walletService,
                                          IPoolService poolService)
        {
            _walletOptions = walletOptions.Value;
            _poolOptions = poolOptions.Value;

            _walletService = walletService;
            _poolService = poolService;
        }

        /// <inheritdoc />
        public virtual async Task<IAgentContext> GetContextAsync(string agentId = null)
        {
            return new DefaultAgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials),
                Pool = await _poolService.GetPoolAsync(_poolOptions.PoolName, _poolOptions.ProtocolVersion)
            };
        }
    }
}
