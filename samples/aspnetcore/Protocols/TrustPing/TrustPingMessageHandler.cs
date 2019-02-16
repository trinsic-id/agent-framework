using System;
using System.Collections.Generic;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Models.Events;

namespace WebAgent.Messages
{
    public class TrustPingMessageHandler : IMessageHandler
    {
        /// <summary>
        /// The event aggregator.
        /// </summary>
        private readonly IEventAggregator _eventAggregator;

        public TrustPingMessageHandler(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<string> SupportedMessageTypes => new[]
        {
            CustomMessageTypes.TrustPingMessageType,
            CustomMessageTypes.TrustPingResponseMessageType
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messagePayload">The agent message agentContext.</param>
        public async Task ProcessAsync(IAgentContext agentContext, MessagePayload messagePayload)
        {
            switch (messagePayload.GetMessageType())
            {
                case CustomMessageTypes.TrustPingMessageType:
                {
                    var pingMessage = messagePayload.GetMessage<TrustPingMessage>();

                        if (pingMessage.ResponseRequested)
                        {
                            if (agentContext is AgentContext context)
                            {
                                context.AddNext(new OutgoingMessage
                                {
                                    OutboundMessage = new TrustPingResponseMessage().ToJson()
                                }.AsMessagePayload());
                            }
                        }
                        break;
                }
                case CustomMessageTypes.TrustPingResponseMessageType:
                {
                    _eventAggregator.Publish(new ServiceMessageProcessingEvent
                    {
                        MessageType = CustomMessageTypes.TrustPingResponseMessageType
                    });
                    break;
                }
            }
        }
    }
}