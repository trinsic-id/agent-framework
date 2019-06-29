using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentFramework.Core.Contracts
{
    public interface IFeesService
    {
        /// <summary>
        /// Gets the fees associated with a given transaction type
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        Task<ulong> GetTransactionFeeAsync(IAgentContext agentContext, string transactionType);
    }
}
