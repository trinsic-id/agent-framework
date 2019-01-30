using System.Collections.Generic;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    public interface IAgentContext
    {
        Wallet Wallet { get; set; }

        Pool Pool { get; set; }

        Dictionary<string, string> State { get; set; }

        ConnectionRecord Connection { get; set; }
    }
}