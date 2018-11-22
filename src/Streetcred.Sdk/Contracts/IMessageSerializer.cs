using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Messages;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Represents a content serializer
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Packs by auth crypting the message and wrapping in an agent wire message
        /// </summary>
        /// <param name="message">The content.</param>
        /// <param name="theirKey">Their myKey.</param>
        /// <returns>A json formatted wire message.</returns>
        Task<string> AnonPackAsync(object message, string theirKey);

        /// <summary>
        /// Packs by auth crypting the message and wrapping in an agent wire message
        /// </summary>
        /// <param name="message">The content.</param>
        /// <param name="theirKey">Their myKey.</param>
        /// <returns>A json formatted wire message.</returns>
        Task<string> AnonPackAsync(string message, string theirKey);

        /// <summary>
        /// Packs by anon crypting the message and wrapping in an agent wire message
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="myKey">My key.</param>
        /// <param name="theirKey">Their key.</param>
        /// <returns>A json formatted wire message.</returns>
        Task<string> AuthPackAsync(Wallet wallet, object message, string myKey, string theirKey);

        /// <summary>
        /// Unpacks Agent wire message and returns the message and the sender key if available
        /// </summary>
        /// <param name="wireMessageJson">Json formatted wire message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <returns>An agent message the sender key and my key if available.</returns>
        Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(string wireMessageJson, Wallet wallet);

        /// <summary>
        /// Unpacks Agent wire message and returns the message and the sender key if available
        /// </summary>
        /// <param name="wireMessageJson">Json formatted wire message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="myKey">My Key.</param>
        /// <returns>An agent message the sender key and my key if available.</returns>
        Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(string wireMessageJson, Wallet wallet, string myKey);

        /// <summary>
        /// Unpacks Agent wire message and returns the message and the sender key if available
        /// </summary>
        /// <param name="content">Json formatted wire message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <returns>An agent message the sender key and my key if available.</returns>
        Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(byte[] content, Wallet wallet);

        /// <summary>
        /// Unpacks Agent wire message and returns the message and the sender key if available
        /// </summary>
        /// <param name="content">Json formatted wire message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="myKey">My Key.</param>
        /// <returns>An agent message the sender key and my key if available.</returns>
        Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(byte[] content, Wallet wallet, string myKey);
    }
}
