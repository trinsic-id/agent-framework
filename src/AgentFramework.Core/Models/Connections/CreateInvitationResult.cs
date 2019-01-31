using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Models.Connections
{
    /// <summary>Result from calling <see cref="IConnectionService.CreateInvitationAsync"/></summary>
    public class CreateInvitationResult
    {
        /// <summary>Gets or sets the invitation.</summary>
        /// <value>The invitation.</value>
        public ConnectionInvitationMessage Invitation { get; set; }

        /// <summary>Gets or sets the connection.</summary>
        /// <value>The connection.</value>
        public ConnectionRecord Connection { get; set; }
    }
}