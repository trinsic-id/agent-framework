using System;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultMessageSerializer : IMessageSerializer
    {
        /// <inheritdoc />
        public virtual async Task<string> AnonPackAsync(string message, string theirKey)
        {
            var messageData = Encoding.UTF8.GetBytes(message);
            var encrypted = await Crypto.AnonCryptAsync(theirKey, messageData);

            return new AgentWireMessage
            {
                To = theirKey,
                Message = Convert.ToBase64String(encrypted)
            }.ToJson();
        }

        /// <inheritdoc />
        public virtual Task<string> AnonPackAsync(object message, string theirKey)
        {
            return AnonPackAsync(message.ToJson(), theirKey);
        }

        /// <inheritdoc />
        public virtual async Task<string> AuthPackAsync(Wallet wallet, object message, string myKey, string theirKey)
        {
            var messageData = Encoding.UTF8.GetBytes(message.ToJson());
            var encrypted = await Crypto.AuthCryptAsync(wallet, myKey, theirKey, messageData);

            return new AgentWireMessage
            {
                To = theirKey,
                From = myKey,
                Message = Convert.ToBase64String(encrypted)
            }.ToJson();
        }

        /// <inheritdoc />
        public virtual Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(byte[] content, Wallet wallet)
        {
            var wireMessage = Encoding.UTF8.GetString(content);
            return UnpackAsync(wireMessage, wallet);
        }

        /// <inheritdoc />
        public virtual Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(byte[] content, Wallet wallet, string myKey)
        {
            var wireMessage = Encoding.UTF8.GetString(content);
            return UnpackAsync(wireMessage, wallet, myKey);
        }

        /// <inheritdoc />
        public virtual async Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(string wireMessageJson, Wallet wallet, string myKey)
        {
            var wireMessage = JsonConvert.DeserializeObject<AgentWireMessage>(wireMessageJson);
            var innerMessage = Convert.FromBase64String(wireMessage.Message);

            IAgentMessage message;
            string theirVerKey = null;

            if (wireMessage.From != null)
            {
                var result = await Crypto.AuthDecryptAsync(wallet, myKey, innerMessage);
                message = JsonConvert.DeserializeObject<IAgentMessage>(Encoding.UTF8.GetString(result.MessageData));
                theirVerKey = result.TheirVk;
            }
            else
            {
                var decrypted = await Crypto.AnonDecryptAsync(wallet, myKey, innerMessage);
                message = JsonConvert.DeserializeObject<IAgentMessage>(Encoding.UTF8.GetString(decrypted));
            }

            return (message, theirVerKey, wireMessage.To);
        }

        /// <inheritdoc />
        public virtual async Task<(IAgentMessage Message, string TheirKey, string MyKey)> UnpackAsync(string wireMessageJson, Wallet wallet)
        {
            var wireMessage = JsonConvert.DeserializeObject<AgentWireMessage>(wireMessageJson);
            var innerMessage = Convert.FromBase64String(wireMessage.Message);

            IAgentMessage message;
            string theirVerKey = null;

            if (wireMessage.From != null)
            {
                var result = await Crypto.AuthDecryptAsync(wallet, wireMessage.To, innerMessage);
                message = JsonConvert.DeserializeObject<IAgentMessage>(Encoding.UTF8.GetString(result.MessageData));
                theirVerKey = result.TheirVk;
            }
            else
            {
                var decrypted = await Crypto.AnonDecryptAsync(wallet, wireMessage.To, innerMessage);
                message = JsonConvert.DeserializeObject<IAgentMessage>(Encoding.UTF8.GetString(decrypted));
            }

            return (message, theirVerKey, wireMessage.To);
        }
    }
}