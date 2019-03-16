using System.Collections.Generic;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Represents an agent context
    /// </summary>
    public interface IAgentContext
    {
        /// <summary>Gets or sets the agent wallet.</summary>
        /// <value>The wallet.</value>
        Wallet Wallet { get; set; }

        /// <summary>Gets or sets the pool.</summary>
        /// <value>The pool.</value>
        PoolAwaitable Pool { get; set; }

        /// <summary>Name/value utility store to pass data
        /// along the execution pipeline.</summary>
        /// <value>The state.</value>
        Dictionary<string, string> State { get; set; }

        /// <summary>If present, gets the connection associated
        /// with the processed message.</summary>
        /// <value>The connection.</value>
        ConnectionRecord Connection { get; set; }
    }
}