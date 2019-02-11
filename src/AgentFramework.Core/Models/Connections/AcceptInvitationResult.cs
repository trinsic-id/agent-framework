using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Models.Connections
{
    /// <summary></summary>
    public class AcceptInvitationResult
    {
        /// <summary>Gets or sets the connection request message that
        /// will be sent back to the invitation party.</summary>
        /// <value>The request.</value>
        public ConnectionRequestMessage Request { get; set; }

        /// <summary>Gets or sets the connection associated with this invitation.</summary>
        /// <value>The connection.</value>
        public ConnectionRecord Connection { get; set; }
        
        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Request={Request}, " +
            $"Connection={Connection}";
    }
}