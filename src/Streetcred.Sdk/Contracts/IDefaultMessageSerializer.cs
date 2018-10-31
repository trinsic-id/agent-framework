using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Represents a content serializer
    /// </summary>
    public interface IDefaultMessageSerializer
    {
        /// <summary>
        /// Packs a content by auth crypting the content and returns a base64 string
        /// </summary>
        /// <param name="message">The content.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="myKey">The myKey.</param>
        /// <param name="theirKey">Their myKey.</param>
        /// <returns></returns>
        Task<T> PackSealedAsync<T>(object message, Wallet wallet, string myKey, string theirKey) where T : IContentMessage, new();

        /// <summary>
        /// Unpacks auth crypted content and returns the content of the message as JSON string and the myKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content">The base64 encoded message content.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="myKey">The myKey.</param>
        /// <returns></returns>
        Task<(T Message, string TheirKey)> UnpackSealedAsync<T>(string content, Wallet wallet, string myKey);

        /// <summary>
        /// Packs a message by anon crypting the serialized content
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        Task<byte[]> PackAsync(object message, string key);

        /// <summary>
        /// Unpacks a message by anon decrypting the serialized content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        Task<T> UnpackAsync<T>(byte[] data, Wallet wallet, string key);

        /// <summary>
        /// Extracts the DID information from the message type
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        (string did, string type) DecodeType(string messageType);

        /// <summary>
        /// Encodes the type.
        /// </summary>
        /// <param name="did">The did.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        string EncodeType(string did, string messageType);
    }
}
