using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Handlers
{
    public static class Extensions
    {
        /// <summary>Wraps the message in payload.</summary>
        /// <param name="agentMessage">The agent message.</param>
        /// <returns></returns>
        public static MessagePayload AsMessagePayload(this IAgentMessage agentMessage) =>
            new MessagePayload(agentMessage);
    }
}