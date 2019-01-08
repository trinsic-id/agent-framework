using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Handlers
{
    public class AgentContext
    {
        public ConnectionRecord Connection { get; set; }

        public Wallet Wallet { get; set; }

        public Pool Pool { get; set; }
    }
}
