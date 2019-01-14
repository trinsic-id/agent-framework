using System;
using System.Collections.Generic;
using System.Linq;
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
            var connectionService = ServiceProvider.GetService<IConnectionService>();

            var outerWireMessage = body.ToObject<AgentWireMessage>();
            var innerWireMessage =
                (await Crypto.AnonDecryptAsync(
                    wallet,
                    outerWireMessage.To,
                    outerWireMessage.Message.GetBytesFromBase64()))
                .ToObject<ForwardMessage>()
                .Message
                .GetBytesFromBase64()
                .ToObject<AgentWireMessage>();

            var authDecrypted = await Crypto.AuthDecryptAsync(
                wallet, innerWireMessage.To, innerWireMessage.Message.GetBytesFromBase64());

            var message = JObject.Parse(authDecrypted.MessageData.GetUTF8String());
            var messageType = message["@type"].ToObject<string>();

            var connectionRecord = await connectionService.ResolveByMyKeyAsync(wallet, innerWireMessage.To);

            var handler = Handlers.FirstOrDefault(x =>
                x.SupportedMessageTypes.Any(y => y.Equals(messageType, StringComparison.OrdinalIgnoreCase)));
            if (handler != null)
            {
                await handler.ProcessAsync(authDecrypted.MessageData.GetUTF8String(),
                    new ConnectionContext {Wallet = wallet, Pool = pool, Connection = connectionRecord});
            }
            else
            {
                throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                    $"Couldn't locate a message handler for type {messageType}");
            }
        }
    }
}