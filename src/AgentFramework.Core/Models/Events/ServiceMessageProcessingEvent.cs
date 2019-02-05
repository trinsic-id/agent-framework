using System;
using System.Collections.Generic;
using System.Text;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Models.Events
{
    /// <summary>
    /// Representation of a message processing event.
    /// </summary>
    public class ServiceMessageProcessingEvent
    {
        /// <summary>
        /// Id of the message.
        /// </summary>
        public string RecordId { get; set; }

        /// <summary>
        /// Agent Message.
        /// </summary>
        public IAgentMessage Message { get; set; }
    }
}
