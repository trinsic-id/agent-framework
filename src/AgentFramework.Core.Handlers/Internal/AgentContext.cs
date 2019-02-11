using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Handlers.Internal
{
    /// <summary>
    /// Agent context that represents the context of a current agent.
    /// </summary>
    public class AgentContext : IAgentContext
    {
        private readonly ConcurrentQueue<MessagePayload> _queue = new ConcurrentQueue<MessagePayload>();

        /// <inheritdoc />
        public Wallet Wallet { get; set; }

        /// <inheritdoc />
        public Pool Pool { get; set; }

        /// <inheritdoc />
        public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public ConnectionRecord Connection { get; set; }

        /// <summary>
        /// Adds a message to the current processing queue
        /// </summary>
        /// <param name="message"></param>
        public void AddNext(MessagePayload message) => _queue.Enqueue(message);

        internal bool TryGetNext(out MessagePayload message) => _queue.TryDequeue(out message);
    }
}