namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// A representaiton of a message type.
    /// </summary>
    public interface IMessageType
    {
        /// <summary>
        /// Base uri the message type derives from.
        /// </summary>
        string BaseUri { get; }

        /// <summary>
        /// Message family the message belongs to.
        /// </summary>
        string MessageFamilyName { get; }

        /// <summary>
        /// Message family uri the message belongs to.
        /// </summary>
        string MessageFamilyUri { get; }

        /// <summary>
        /// Message name of the message.
        /// </summary>
        string MessageName { get; }

        /// <summary>
        /// Full Uri of the message type.
        /// </summary>
        string MessageTypeUri { get; }

        /// <summary>
        /// Message version the message belongs to.
        /// </summary>
        string MessageVersion { get; }
    }
}