using System;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Contracts;

namespace Streetcred.Sdk.Runtime
{
    public class MessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Packs a content by auth crypting the content and returns a base64 string
        /// </summary>
        /// <param name="message">The content.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="myKey">The myKey.</param>
        /// <param name="theirKey">Their myKey.</param>
        /// <returns></returns>
        public async Task<T> PackSealedAsync<T>(object message, Wallet wallet, string myKey, string theirKey)
            where T : IContentMessage, new()
        {
            var json = JsonConvert.SerializeObject(message);
            var messageData = Encoding.UTF8.GetBytes(json);
            var encrypted = await Crypto.AuthCryptAsync(wallet, myKey, theirKey, messageData);
            
            return new T { Content = Convert.ToBase64String(encrypted) };
        }

        /// <summary>
        /// Unpacks auth crypted content and returns the content of the message as JSON string and the myKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content">The base64 encoded message content.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="myKey">The myKey.</param>
        /// <returns></returns>
        public async Task<(T Message, string TheirKey)> UnpackSealedAsync<T>(string content, Wallet wallet, string myKey)
        {
            var decoded = Convert.FromBase64String(content);
            var decrypted = await Crypto.AuthDecryptAsync(wallet, myKey, decoded);

            var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decrypted.MessageData));
            return (message, decrypted.TheirVk);
        }

        /// <summary>
        /// Wraps the asynchronous.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task<byte[]> PackAsync(object message, string key)
        {
            var json = JsonConvert.SerializeObject(message);
            var messageData = Encoding.UTF8.GetBytes(json);
            var encrypted = await Crypto.AnonCryptAsync(key, messageData);

            return encrypted;
        }

        /// <summary>
        /// Unwraps the asynchronous.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task<T> UnpackAsync<T>(byte[] data, Wallet wallet, string key)
        {
            var decrypted = await Crypto.AnonDecryptAsync(wallet, key, data);

            var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decrypted));
            return message;
        }

        public (string did, string type) DecodeType(string messageType)
        {
            throw new NotImplementedException();
        }

        public string EncodeType(string did, string messageType)
        {
            throw new NotImplementedException();
        }
    }
}
