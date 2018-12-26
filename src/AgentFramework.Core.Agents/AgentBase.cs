using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Agents
{
    public abstract class AgentBase
    {
        public IServiceProvider ServiceProvider { get; }

        protected AgentBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public abstract IEnumerable<IHandler> Handlers { get; }

        public async Task ProcessAsync(byte[] body, Wallet wallet, Pool pool = null)
        {
            var messageSerializer = ServiceProvider.GetService<IMessageSerializer>();
            var connectionService = ServiceProvider.GetService<IConnectionService>();

            var outerMessage =
                await messageSerializer.AnonUnpackAsync(body, wallet);

            var forwardMessage = outerMessage as ForwardMessage ?? throw new Exception("Expected inner message to be of type 'ForwardMessage'");

            var innerMessageContents = Convert.FromBase64String(forwardMessage.Message);

            var wireMessageJson = Encoding.UTF8.GetString(innerMessageContents);
            var wireMessage = JsonConvert.DeserializeObject<AgentWireMessage>(wireMessageJson);

            var result = await Crypto.AuthDecryptAsync(wallet, wireMessage.To, Convert.FromBase64String(wireMessage.Message));
            var messageData = Encoding.UTF8.GetString(result.MessageData);

            var connectionRecord = await connectionService.ResolveByMyKeyAsync(wallet, wireMessage.To);

            var jmessage = JObject.Parse(messageData);
            var messageType = jmessage["@type"].ToObject<string>();

            var handler = Handlers.FirstOrDefault(x =>
                x.SupportedMessageTypes.Any(y => y.Equals(messageType, StringComparison.OrdinalIgnoreCase)));
            if (handler != null)
            {
                await handler.OnMessageAsync(messageData, new AgentContext { Wallet = wallet, Pool = pool, Connection = connectionRecord});
            }
        }
    }
}
