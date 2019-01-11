using System;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultMessageSerializer : IMessageSerializer
    {
        private async Task<byte[]> AnonPackAsync(string message, string theirKey)
        {
            var messageData = Encoding.UTF8.GetBytes(message);
            var encrypted = await Crypto.AnonCryptAsync(theirKey, messageData);

            return Encoding.UTF8.GetBytes(new AgentWireMessage
            {
                To = theirKey,
                Message = Convert.ToBase64String(encrypted)
            }.ToJson());
        }

        /// <inheritdoc />
        public virtual Task<byte[]> AnonPackAsync(IAgentMessage message, string theirKey) => AnonPackAsync(message.ToJson(), theirKey);

        /// <inheritdoc />
        public virtual async Task<byte[]> AuthPackAsync(Wallet wallet, IAgentMessage message, string myKey, string theirKey)
        {
            var messageData = Encoding.UTF8.GetBytes(message.ToJson());
            var encrypted = await Crypto.AuthCryptAsync(wallet, myKey, theirKey, messageData);

            return Encoding.UTF8.GetBytes(new AgentWireMessage
            {
                To = theirKey,
                From = myKey,
                Message = Convert.ToBase64String(encrypted)
            }.ToJson());
        }

        /// <inheritdoc />
        public virtual async Task<(IAgentMessage Message, string TheirKey, string MyKey)> AuthUnpackAsync(byte[] content, Wallet wallet)
        {
            var wireMessageJson = Encoding.UTF8.GetString(content);
            var wireMessage = JsonConvert.DeserializeObject<AgentWireMessage>(wireMessageJson);
            var innerMessage = Convert.FromBase64String(wireMessage.Message);

            var result = await Crypto.AuthDecryptAsync(wallet, wireMessage.To, innerMessage);
            var message = JsonConvert.DeserializeObject<ForwardMessage>(Encoding.UTF8.GetString(result.MessageData));
            var theirVerKey = result.TheirVk;
            
            return (message, theirVerKey, wireMessage.To);
        }
        
        /// <inheritdoc />
        public async Task<IAgentMessage> AnonUnpackAsync(byte[] content, Wallet wallet)
        {
            var wireMessageJson = Encoding.UTF8.GetString(content);
            var wireMessage = JsonConvert.DeserializeObject<AgentWireMessage>(wireMessageJson);
            var innerMessage = Convert.FromBase64String(wireMessage.Message);

            var result = await Crypto.AnonDecryptAsync(wallet, wireMessage.To, innerMessage);
            return JsonConvert.DeserializeObject<ForwardMessage>(Encoding.UTF8.GetString(result));
        }
    }
}