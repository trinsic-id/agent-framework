using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Models;
using Microsoft.Extensions.Options;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultAgentProvider : IAgentProvider
    {
        private readonly WalletOptions _walletOptions;
        private readonly PoolOptions _poolOptions;
        private readonly IAgent _defaultAgent;
        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;

        /// <summary>
        /// Default Agent Context Provider.
        /// </summary>
        /// <param name="walletOptions">The wallet options provider.</param>
        /// <param name="poolOptions">The pool options provider/</param>
        /// <param name="walletService">The wallet service.</param>
        /// <param name="poolService">The pool service.</param>
        public DefaultAgentProvider(IOptions<WalletOptions> walletOptions,
                                          IOptions<PoolOptions> poolOptions,
                                          IAgent serviceProvider,
                                          IWalletService walletService,
                                          IPoolService poolService)
        {
            _walletOptions = walletOptions.Value;
            _poolOptions = poolOptions.Value;
            _defaultAgent = serviceProvider;
            _walletService = walletService;
            _poolService = poolService;
        }

        /// <inheritdoc />
        public Task<IAgent> GetAgentAsync(string agentId = null)
        {
            return Task.FromResult(_defaultAgent);
        }

        /// <inheritdoc />
        public async Task<IAgentContext> GetContextAsync(string agentId = null)
        {
            return new DefaultAgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials),
                Pool = new PoolAwaitable(() => _poolService.GetPoolAsync(
                    _poolOptions.PoolName, _poolOptions.ProtocolVersion))
            };
        }
    }
}
