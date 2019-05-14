using System.Threading.Tasks;
using AgentFramework.Core.Contracts;

namespace AgentFramework.Core.Handlers.Agents
{
    /// <summary>
    /// Agent Context Provider.
    /// </summary>
    public interface IAgentProvider
    {
        /// <summary>
        /// Retrieves an agent context.
        /// </summary>
        /// <param name="agentId">Identifier of the agent to resolve.</param>
        /// <returns>The agent context async.</returns>
        Task<IAgentContext> GetContextAsync(params object[] args);

        /// <summary>
        /// Returns an instance of <see cref="IAgent" />
        /// </summary>
        /// <returns>The agent instance.</returns>
        /// <param name="agentId">Agent identifier.</param>
        Task<IAgent> GetAgentAsync(params object[] args);
    }
}