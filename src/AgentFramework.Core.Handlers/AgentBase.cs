using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Base agent implementation
    /// </summary>
    public abstract class AgentMessageProcessorBase
    {
        private readonly IList<IMessageHandler> _handlers;

        /// <summary>Gets the provider.</summary>
        /// <value>The provider.</value>
        protected IServiceProvider Provider { get; }

        /// <summary>Gets the connection service.</summary>
        /// <value>The connection service.</value>
        protected IConnectionService ConnectionService { get; }

        /// <summary>Gets the message service.</summary>
        /// <value>The message service.</value>
        protected IMessageService MessageService { get; }

        /// <summary>Gets the logger.</summary>
        /// <value>The logger.</value>
        protected ILogger<AgentMessageProcessorBase> Logger { get; }

        /// <summary>Initializes a new instance of the <see cref="AgentMessageProcessorBase"/> class.</summary>
        protected AgentMessageProcessorBase(IServiceProvider provider)
        {
            Provider = provider;
            ConnectionService = provider.GetRequiredService<IConnectionService>();
            MessageService = provider.GetRequiredService<IMessageService>();
            Logger = provider.GetRequiredService<ILogger<AgentMessageProcessorBase>>();
            _handlers = new List<IMessageHandler>();
        }

        /// <summary>Adds a handler for supporting default connection flow.</summary>
        protected void AddConnectionHandler() => _handlers.Add(Provider.GetRequiredService<DefaultConnectionHandler>());

        /// <summary>Adds a handler for supporting default credential flow.</summary>
        protected void AddCredentialHandler() => _handlers.Add(Provider.GetRequiredService<DefaultCredentialHandler>());

        /// <summary>Adds the handler for supporting default proof flow.</summary>
        protected void AddProofHandler() => _handlers.Add(Provider.GetRequiredService<DefaultProofHandler>());

        /// <summary>Adds a default forwarding handler.</summary>
        protected void AddForwardHandler() => _handlers.Add(Provider.GetRequiredService<DefaultForwardHandler>());

        /// <summary>Adds a default forwarding handler.</summary>
        protected void AddEphemeralChallengeHandler() => _handlers.Add(Provider.GetRequiredService<DefaultEphemeralChallengeHandler>());

        /// <summary>Adds a custom the handler using dependency injection.</summary>
        /// <typeparam name="T"></typeparam>
        protected void AddHandler<T>() where T : IMessageHandler => _handlers.Add(Provider.GetRequiredService<T>());

        /// <summary>Adds an instance of a custom handler.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        protected void AddHandler<T>(T instance) where T : IMessageHandler => _handlers.Add(instance);

        /// <summary>
        /// Invoke the handler pipeline and process the passed message.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="context">The agent context.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Expected inner message to be of type 'ForwardMessage'</exception>
        /// <exception cref="AgentFrameworkException">Couldn't locate a message handler for type {messageType}</exception>
        protected async Task<byte[]> ProcessAsync(byte[] body, IAgentContext context)
        {
            EnsureConfigured();

            var agentContext = context.ToHandlerAgentContext();
            agentContext.AddNext(new MessagePayload(body, true));

            AgentMessage outgoingMessage = null;
            while (agentContext.TryGetNext(out var message) && outgoingMessage == null)
            {
                outgoingMessage = await ProcessMessage(agentContext, message);
            }

            if (outgoingMessage != null)
            {
                //TODO error handling what happens when this message fails to transmit? #70
                await MessageService.SendToConnectionAsync(agentContext.Wallet, outgoingMessage,
                    agentContext.Connection);
            }

            //TODO return a result when the request asked for duplex communication #123
            return null;
        }

        private async Task<AgentMessage> ProcessMessage(IAgentContext agentContext, MessagePayload message)
        {
            MessagePayload messagePayload;
            if (message.Packed)
            {
                var unpacked = await CryptoUtils.UnpackAsync(agentContext.Wallet, message.Payload);
                Logger.LogInformation($"Agent Message Received : {unpacked.Message}");
                messagePayload = new MessagePayload(unpacked.Message, false);
                if (unpacked.SenderVerkey != null && agentContext.Connection == null)
                {
                    try
                    {
                        agentContext.Connection = await ConnectionService.ResolveByMyKeyAsync(
                            agentContext, unpacked.RecipientVerkey);
                    }
                    catch (AgentFrameworkException ex) when (ex.ErrorCode == ErrorCode.RecordNotFound)
                    {
                        // OK if not resolved. Example: authpacked forward message in routing agent.
                        // Downstream consumers should throw if Connection is required
                    }
                }
            }
            else
            {
                messagePayload = message;
            }

            if (_handlers.Where(handler => handler != null).FirstOrDefault(
                    handler => handler.SupportedMessageTypes.Any(
                        type => type.Equals(messagePayload.GetMessageType(), StringComparison.OrdinalIgnoreCase))) is
                IMessageHandler messageHandler)
            {
                Logger.LogDebug("Processing message type {MessageType}, {MessageData}", messagePayload.GetMessageType(),
                    messagePayload.Payload.GetUTF8String());
                var outboundMessage = await messageHandler.ProcessAsync(agentContext, messagePayload);
                return outboundMessage;
            }

            throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                $"Couldn't locate a message handler for type {messagePayload.GetMessageType()}");
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