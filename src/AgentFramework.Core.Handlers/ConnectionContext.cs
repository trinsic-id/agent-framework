using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Connection context that represents a secure connection between two parties
    /// </summary>
    public class ConnectionContext
    {
        /// <summary>
        /// Gets or sets the connection associated with this message.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public ConnectionRecord Connection { get; set; }

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
