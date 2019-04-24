using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// A message context object that surrounds an agent message
    /// </summary>
    public interface IMessageContext
    {
        /// <summary>
        /// The raw format of the message.
        /// </summary>
        byte[] Payload { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IMessageContext"/> is packed.
        /// </summary>
        bool Packed { get; }

        /// <summary>
        /// Gets the connection associated to the message.
        /// </summary>
        ConnectionRecord Connection { get; }

        /// <summary>
        /// The message id of the current message.
        /// </summary>
        string GetMessageId();

        /// <summary>
        /// The message type of the current message.
        /// </summary>
        string GetMessageType();

        /// <summary>
        /// Gets the message cast to the expect message type.
        /// </summary>
        /// <typeparam name="T">The generic type the message will be cast to.</typeparam>
        T GetMessage<T>() where T : AgentMessage, new();

        /// <summary>
        /// Gets the message cast to the expect message type.
        /// </summary>
        string GetMessageJson();
    }
}
