using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Represents a content serializer
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Packs by auth crypting the message and wrapping in an agent wire message.
        /// </summary>
        /// <param name="message">The agent message.</param>
        /// <param name="theirKey">Their myKey.</param>
        /// <returns>A UTF8 byte array representing a json formatted wire message.</returns>
        Task<byte[]> AnonPackAsync(IAgentMessage message, string theirKey);

        /// <summary>
        /// Packs by auth crypting the message and wrapping in an agent wire message.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="myKey">My key.</param>
        /// <param name="theirKey">Their key.</param>
        /// <returns>A UTF8 byte array representing a json formatted wire message.</returns>
        Task<byte[]> AuthPackAsync(Wallet wallet, IAgentMessage message, string myKey, string theirKey);

        /// <summary>
        /// Unpacks by auth decrypting an agent wire message.
        /// </summary>
        /// <param name="content">A UTF8 byte array of a json formatted wire message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <returns>An agent message, the sender key, my key.</returns>
        Task<(IAgentMessage Message, string TheirKey, string MyKey)> AuthUnpackAsync(byte[] content, Wallet wallet);
        
        /// <summary>
        /// Unpacks by anon decrypting an agent wire message.
        /// </summary>
        /// <param name="content">A UTF8 byte array of a json formatted wire message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <returns>An agent message.</returns>
        Task<IAgentMessage> AnonUnpackAsync(byte[] content, Wallet wallet);
    }
}
