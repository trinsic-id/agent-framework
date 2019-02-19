using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Base agent implementation
    /// </summary>
    public abstract class AgentBase
    {
        private readonly IList<IMessageHandler> _handlers;
        private ILogger<AgentBase> _logger;

        /// <summary>Gets the provider.</summary>
        /// <value>The provider.</value>
        protected IServiceProvider Provider { get; }

        /// <summary>Gets the connection service.</summary>
        /// <value>The connection service.</value>
        protected IConnectionService ConnectionService { get; }

        /// <summary>Gets the logger.</summary>
        /// <value>The logger.</value>
        protected ILogger<AgentBase> Logger { get; }

        /// <summary>Initializes a new instance of the <see cref="AgentBase"/> class.</summary>
        protected AgentBase(IServiceProvider provider, IConnectionService connectionService, ILogger<AgentBase> logger)
        {
            Provider = provider;
            ConnectionService = connectionService;
            Logger = logger;
            _handlers = new List<IMessageHandler>();

            _logger = provider.GetService<ILogger<AgentBase>>();
        }

        /// <summary>Adds a handler for supporting default connection flow.</summary>
        protected void AddConnectionHandler()
        {
            _handlers.Add(new DefaultConnectionHandler(Provider.GetService<IConnectionService>()));
            _handlers.Add(new OutgoingMessageHandler(Provider.GetServices<IOutgoingMessageDecoratorHandler>()));
            _handlers.Add(new HttpOutgoingMessageHandler(Provider.GetService<HttpClientHandler>() ?? new HttpClientHandler()));
        }
        /// <summary>Adds a handler for supporting default credential flow.</summary>
        protected void AddCredentialHandler()
        {
            _handlers.Add(new DefaultCredentialHandler(Provider.GetService<ICredentialService>()));
        }
        /// <summary>Adds the handler for supporting default proof flow.</summary>
        protected void AddProofHandler()
        {
            _handlers.Add(new DefaultProofHandler(Provider.GetService<IProofService>()));
        }
        /// <summary>Adds a default forwarding handler.</summary>
        protected void AddForwardHandler()
        {
            _handlers.Add(new DefaultForwardHandler(Provider.GetService<IConnectionService>()));
        }

        /// <summary>Adds a custom the handler using dependency injection.</summary>
        /// <typeparam name="T"></typeparam>
        protected void AddHandler<T>() where T : IMessageHandler => _handlers.Add(Provider.GetService<T>());

        /// <summary>Adds an instance of a custom handler.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        protected void AddHandler<T>(T instance) where T : IMessageHandler => _handlers.Add(instance);

        /// <summary>
        /// Invoke the handler pipeline and process the passed message.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="wallet">The wallet.</param>
        /// <param name="pool">The pool.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Expected inner message to be of type 'ForwardMessage'</exception>
        /// <exception cref="AgentFrameworkException">Couldn't locate a message handler for type {messageType}</exception>
        protected async Task ProcessAsync(byte[] body, Wallet wallet, Pool pool = null)
        {
            EnsureConfigured();

            var agentContext = new AgentContext {Wallet = wallet, Pool = pool};
            agentContext.AddNext(new MessagePayload(body, true));

            while (agentContext.TryGetNext(out var message))
            {
                MessagePayload messagePayload;
                if (message.Packed)
                {
                    var unpacked = await CryptoUtils.UnpackAsync(agentContext.Wallet, message.Payload);
                    Logger.LogInformation($"Agent Message Recieved : {unpacked.Message}");
                    messagePayload = new MessagePayload(unpacked.Message, false);
                    if (unpacked.SenderVerkey != null && agentContext.Connection == null)
                    {
                        agentContext.Connection = await ConnectionService.ResolveByMyKeyAsync(agentContext, unpacked.RecipientVerkey);
                    }
                }
                else
                {
                    messagePayload = message;
                }

                if (_handlers.Where(handler => handler != null).FirstOrDefault(
                        handler => handler.SupportedMessageTypes.Any(
                            type => type.Equals(messagePayload.GetMessageType(), StringComparison.OrdinalIgnoreCase))) is IMessageHandler messageHandler)
                {
                    _logger.LogDebug("Processing message type {MessageType}, {MessageData}", messagePayload.GetMessageType(), messagePayload.Payload.GetUTF8String());
                    await messageHandler.ProcessAsync(agentContext, messagePayload);
                }
                else
                {
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Couldn't locate a message handler for type {messagePayload.GetMessageType()}");
                }
            }
        }

        private void EnsureConfigured()
        {
            if (_handlers == null || !_handlers.Any())
                ConfigureHandlers();
        }

        /// <summary>Configures the handlers.</summary>
        protected virtual void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddForwardHandler();
        }
    }
}