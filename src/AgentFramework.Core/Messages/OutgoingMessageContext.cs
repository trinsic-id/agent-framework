using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework.Core.Messages
{
    /// <summary>
    /// A wrapper around an outbound message, pairing it with the inbound message if applicable.
    /// </summary>
    public class OutgoingMessageContext
    {
        /// <summary>
        /// Default constructor for message pair.
        /// </summary>
        /// <param name="inboundMessage">The inbound message.</param>
        /// <param name="outboundMessage">The outbound message.</param>
        public OutgoingMessageContext(AgentMessage inboundMessage, AgentMessage outboundMessage)
        {
            InboundMessage = inboundMessage;
            OutboundMessage = outboundMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outboundMessage"></param>
        public OutgoingMessageContext(AgentMessage outboundMessage)
        {
            OutboundMessage = outboundMessage;
        }

        /// <summary>
        /// The inbound message.
        /// </summary>
        public AgentMessage InboundMessage { get; }

        /// <summary>
        /// The outbound message.
        /// </summary>
        public AgentMessage OutboundMessage { get; }
    }
}
