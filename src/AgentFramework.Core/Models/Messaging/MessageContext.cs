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
        private readonly JObject _messageJson;

        /// <summary>Initializes a new instance of the <see cref="MessageContext"/> class.</summary>
        /// <param name="messageData">The message data.</param>
        /// <param name="agentContext">The agent context.</param>
        /// <param name="connection">The connection.</param>
        public MessageContext(byte[] messageData, AgentContext agentContext, ConnectionRecord connection)
        {
            MessageData = messageData;
            AgentContext = agentContext;
            Connection = connection;

            _messageJson = JObject.Parse(messageData.GetUTF8String());
        }

        /// <summary>Initializes a new instance of the <see cref="MessageContext"/> class.</summary>
        /// <param name="messageData">The message data.</param>
        /// <param name="agentContext">The agent context.</param>
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
        public string MessageType => _messageJson["@type"].ToObject<string>();

        /// <summary>
        /// Gets the message cast to the expect message type.
        /// </summary>
        /// <typeparam name="T">The generic type the message will be cast to.</typeparam>
        /// <returns>The agent message.</returns>
        public T GetMessage<T>() where T : IAgentMessage => _messageJson.ToObject<T>();

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
