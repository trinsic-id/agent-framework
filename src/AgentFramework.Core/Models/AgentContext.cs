using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Models
{
    /// <summary>
    /// Agent context that represents the context of a current agent.
    /// </summary>
    public class AgentContext
    {
        /// <summary>
        /// Gets or sets the wallet.
        /// </summary>
        /// <value>
        /// The wallet.
        /// </value>
        public Wallet Wallet { get; set; }

        /// <summary>
        /// Gets or sets the pool.
        /// </summary>
        /// <value>
        /// The pool.
        /// </value>
        public Pool Pool { get; set; }
    }
}
