using System;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// A message context object that surrounds an agent message
    /// </summary>
    public class MessagePayload
    {
        public bool Packed { get; }
        private JObject _messageJson;
        
        public MessagePayload(byte[] message, bool packed)
        {
            Packed = packed;
            Payload = message;
            if (!Packed) _messageJson = JObject.Parse(Payload.GetUTF8String());
        }

        public MessagePayload(string message, bool packed)
        {
            Packed = packed;
            Payload = message.GetUTF8Bytes();
            _messageJson = JObject.Parse(message);
        }

        public MessagePayload(IAgentMessage message)
        : this(message.ToJson(), false)
        {

        }

        /// <summary>
        /// The raw format of the message.
        /// </summary>
        internal byte[] Payload { get; }

        /// <summary>
        /// The message type of the current message.
        /// </summary>
        public string GetMessageType() =>
            Packed
                ? throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot deserialize packed message.")
                : _messageJson["@type"].Value<string>();

        /// <summary>
        /// Gets the message cast to the expect message type.
        /// </summary>
        /// <typeparam name="T">The generic type the message will be cast to.</typeparam>
        /// <returns>The agent message.</returns>
        public T GetMessage<T>() where T : IAgentMessage =>
            Packed
                ? throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot deserialize packed message.")
                : _messageJson.ToObject<T>();

        /// <summary>
        /// Gets the decorator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Cannot deserialize packed message.</exception>
        public T GetDecorator<T>(string name) where T : IAgentMessage =>
            Packed
                ? throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot deserialize packed message.")
                : _messageJson[$"~{name}"].ToObject<T>();

    }
}
