using AgentFramework.Core.Contracts;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Messages
{
    /// <summary>
    /// A representaiton of a message type.
    /// </summary>
    public class MessageType : IMessageType
    {
        /// <summary>
        /// Constructor for creating the representation from a message type uri.
        /// </summary>
        /// <param name="messageTypeUri">Message type uri.</param>
        public MessageType(string messageTypeUri)
        {
            var (uri, messageFamilyName, messageVersion, messageName) = MessageUtils.DecodeMessageTypeUri(messageTypeUri);

            BaseUri = uri;
            MessageFamilyName = messageFamilyName;
            MessageVersion = messageVersion;
            MessageName = messageName;
            MessageTypeUri = messageTypeUri;
        }

        /// <summary>
        /// Base uri the message type derives from.
        /// </summary>
        public string BaseUri { get; }

        /// <summary>
        /// Message family the message belongs to.
        /// </summary>
        public string MessageFamilyName { get; }

        /// <summary>
        /// Message family uri the message belongs to.
        /// </summary>
        public string MessageFamilyUri => $"{BaseUri}/{MessageFamilyName}/{MessageVersion}";

        /// <summary>
        /// Message version the message belongs to.
        /// </summary>
        public string MessageVersion { get; }

        /// <summary>
        /// Message name of the message.
        /// </summary>
        public string MessageName { get; }

        /// <summary>
        /// Full Uri of the message type.
        /// </summary>
        public string MessageTypeUri { get; }
    }
}
