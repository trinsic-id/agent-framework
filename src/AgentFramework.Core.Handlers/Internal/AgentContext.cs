using System.Collections.Concurrent;
using System.Collections.Generic;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Handlers.Internal
{
    /// <summary>
    /// Agent context that represents the context of a current agent.
    /// </summary>
    internal class AgentContext : IAgentContext
    {
        private readonly ConcurrentQueue<MessagePayload> _queue = new ConcurrentQueue<MessagePayload>();

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

        public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>();

        /// <summary>Gets or sets the connection.</summary>
        /// <value>The connection.</value>
        public ConnectionRecord Connection { get; set; }

        internal void AddNext(MessagePayload message) => _queue.Enqueue(message);

        internal bool TryGetNext(out MessagePayload message) => _queue.TryDequeue(out message);
    }
}