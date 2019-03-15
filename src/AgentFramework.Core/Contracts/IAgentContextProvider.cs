using System.Threading.Tasks;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Agent Context Provider.
    /// </summary>
    public interface IAgentContextProvider
    {
        /// <summary>
        /// Retrieves an agent context.
        /// </summary>
        /// <param name="agentId">Identifier of the agent to resolve.</param>
        /// <returns>The agent context async.</returns>
        Task<IAgentContext> GetContextAsync(string agentId = null);
    }
}