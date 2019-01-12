using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Base agent implementation
    /// </summary>
    public abstract class AgentBase
    {
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>
        /// The service provider.
        /// </value>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentBase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        protected AgentBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the message handlers.
        /// </summary>
        /// <value>
        /// The handlers.
        /// </value>
        public abstract IEnumerable<IMessageHandler> Handlers { get; }

        /// <summary>
        /// Processes the asynchronous.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Expected inner message to be of type 'ForwardMessage'</exception>
        /// <exception cref="AgentFrameworkException">Couldn't locate a message handler for type {messageType}</exception>
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
            
            var jmessage = JObject.Parse(messageData);
            var messageType = jmessage["@type"].ToObject<string>();

            var connectionRecord = await connectionService.ResolveByMyKeyAsync(wallet, wireMessage.To);

            var handler = Handlers.FirstOrDefault(x =>
                x.SupportedMessageTypes.Any(y => y.Equals(messageType, StringComparison.OrdinalIgnoreCase)));
            if (handler != null)
            {
                await handler.ProcessAsync(messageData, new ConnectionContext { Wallet = wallet, Pool = pool, Connection = connectionRecord});
            }
            else
            {
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, $"Couldn't locate a message handler for type {messageType}");
            }
        }
    }
}
