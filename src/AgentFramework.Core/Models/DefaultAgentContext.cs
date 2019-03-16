using System.Collections.Generic;
using System.Text;
using System.Threading;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Models
{
    /// <inheritdoc />
    /// <summary>
    /// Default Agent Context Object.
    /// </summary>
    public class DefaultAgentContext : IAgentContext
    {
        /// <inheritdoc />
        /// <summary>
        /// The agent context wallet,
        /// </summary>
        public Wallet Wallet { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// The agent context pool.
        /// </summary>
        public PoolAwaitable Pool { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// The agent context state.
        /// </summary>
        public Dictionary<string, string> State { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// The current connection associated to the agent context.
        /// </summary>
        public ConnectionRecord Connection { get; set; }
    }
}
