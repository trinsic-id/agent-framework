using System;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class ABaseMessageSerializer : IMessageSerializer
    {
        /// <inheritdoc />
        public virtual async Task<T> PackSealedAsync<T>(object message, Wallet wallet, string myKey, string theirKey)
            where T : IContentMessage, new()
        {
            var messageData = Encoding.UTF8.GetBytes(message.ToJson());
            var encrypted = await Crypto.AuthCryptAsync(wallet, myKey, theirKey, messageData);

            return new T {Content = Convert.ToBase64String(encrypted)};
        }

        /// <inheritdoc />
        public virtual async Task<(T Message, string TheirKey)> UnpackSealedAsync<T>(string content, Wallet wallet,
            string myKey)
        {
            var decoded = Convert.FromBase64String(content);
            var decrypted = await Crypto.AuthDecryptAsync(wallet, myKey, decoded);

            var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decrypted.MessageData));
            return (message, decrypted.TheirVk);
        }

        /// <inheritdoc />
        public virtual async Task<byte[]> PackAsync(object message, string key)
        {
            var messageData = Encoding.UTF8.GetBytes(message.ToJson());
            var encrypted = await Crypto.AnonCryptAsync(key, messageData);

            return encrypted;
        }

        /// <inheritdoc />
        public virtual async Task<T> UnpackAsync<T>(byte[] data, Wallet wallet, string key)
        {
            var decrypted = await Crypto.AnonDecryptAsync(wallet, key, data);

            var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decrypted));
            return message;
        }

        public virtual (string did, string type) DecodeType(string messageType)
        {
            throw new NotImplementedException();
        }

        public virtual string EncodeType(string did, string messageType)
        {
            throw new NotImplementedException();
        }
    }
}