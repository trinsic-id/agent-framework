﻿using System;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Messages
{
    /// <summary>
    /// A message context object that surrounds an agent message
    /// </summary>
    public class MessagePayload
    {
        /// <summary>Gets a value indicating whether this <see cref="MessagePayload"/> is packed.</summary>
        /// <value>
        ///   <c>true</c> if packed; otherwise, <c>false</c>.</value>
        public bool Packed { get; }

        private readonly JObject _messageJson;

        /// <summary>Initializes a new instance of the <see cref="MessagePayload"/> class.</summary>
        /// <param name="message">The message.</param>
        /// <param name="packed">if set to <c>true</c> [packed].</param>
        public MessagePayload(byte[] message, bool packed)
        {
            Packed = packed;
            Payload = message;
            if (!Packed) _messageJson = JObject.Parse(Payload.GetUTF8String());
        }

        /// <inheritdoc />
        public MessagePayload(string message, bool packed) : this(message.GetUTF8Bytes(), packed)
        {
        }

        /// <inheritdoc />
        /// <param name="message">The message.</param>
        public MessagePayload(IAgentMessage message)
        : this(message.ToJson(), false)
        {
        }

        /// <summary>
        /// The raw format of the message.
        /// </summary>
        internal byte[] Payload { get; }

        /// <summary>
        /// The message id of the current message.
        /// </summary>
        public string GetMessageId() =>
            Packed
                ? throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot deserialize packed message.")
                : _messageJson["@id"].Value<string>();

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
        /// <exception cref="AgentFrameworkException">ErrorCode.InvalidMessage Cannot deserialize packed message or unable to find decorator on message.</exception>
        public T GetDecorator<T>(string name) where T : class
        {
            if (Packed)
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot deserialize packed message.");
            try
            {
                return _messageJson[$"~{name}"].ToObject<T>();
            }
            catch (Exception e)
            {
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Failed to extract decorator from message", e);
            }
        }

        /// <summary>
        /// Adds the decorator.
        /// </summary>
        /// <param name="decorator">The decorator.</param>
        /// <param name="name">The decorator name.</param>
        /// <exception cref="AgentFrameworkException">Cannot add a decorator to a packed message.</exception>
        public void AddDecorator<T>(T decorator, string name) where T : class
        {
            if (Packed)
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Cannot add a decorator to a packed message");

            _messageJson.Add($"~{name}", JsonConvert.DeserializeObject<JToken>(decorator.ToJson()));
        }
    }
}
