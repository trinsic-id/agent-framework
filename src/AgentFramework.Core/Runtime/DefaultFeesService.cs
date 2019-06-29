using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;

namespace AgentFramework.Core.Runtime
{
    public class DefaultFeesService : IFeesService
    {

        public Task<ulong> GetTransactionFeeAsync(IAgentContext agentContext, string transactionType)
        {
            return Task.FromResult(0UL);
        }

        public Task<IDictionary<string, ulong>> GetTransactionFeesAsync(IAgentContext agentContext)
        {
            throw new NotImplementedException();
        }
    }
}
