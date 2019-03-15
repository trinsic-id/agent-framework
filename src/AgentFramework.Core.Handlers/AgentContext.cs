using System.Collections.Concurrent;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Agent context that represents the context of a current agent.
    /// </summary>
    public class AgentContext : DefaultAgentContext
    {
        private readonly ConcurrentQueue<MessagePayload> _queue = new ConcurrentQueue<MessagePayload>();
        
        /// <summary>
        /// Adds a message to the current processing queue
        /// </summary>
        /// <param name="message"></param>
        public void AddNext(MessagePayload message) => _queue.Enqueue(message);

        internal bool TryGetNext(out MessagePayload message) => _queue.TryDequeue(out message);
    }
}