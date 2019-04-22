using System.Collections.Generic;
using AgentFramework.Core.Contracts;
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
    }
}
