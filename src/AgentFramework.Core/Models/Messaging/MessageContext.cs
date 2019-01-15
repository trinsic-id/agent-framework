using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Models.Messaging
{
    /// <summary>
    /// A message context object that surrounds an agent message
    /// </summary>
    public class MessageContext
    {
        public MessageContext(byte[] messageData, AgentContext agentContext, ConnectionRecord connection)
        {
            MessageData = messageData;
            AgentContext = agentContext;
            Connection = connection;
        }

        public MessageContext(byte[] messageData, AgentContext agentContext)
        {
            MessageData = messageData;
            AgentContext = agentContext;
        }

        /// <summary>
        /// The raw format of the message.
        /// </summary>
        public byte[] MessageData { get; }

        /// <summary>
        /// The message type of the current message.
        /// </summary>
        public string MessageType => JObject.Parse(MessageData.GetUTF8String())["@type"].ToObject<string>();

        /// <summary>
        /// Gets the message cast to the expect message type.
        /// </summary>
        /// <typeparam name="T">The generic type the message will be cast to.</typeparam>
        /// <returns>The agent message.</returns>
        public T GetMessage<T>() where T : IAgentMessage => MessageData.ToObject<T>();

        /// <summary>
        /// The associated connection to the agent message.
        /// </summary>
        public ConnectionRecord Connection { get; }

        /// <summary>
        /// The associated agent context to the message.
        /// </summary>
        public AgentContext AgentContext { get; set; }

        //TODO here is where the ThreadContext will live
    }
}
