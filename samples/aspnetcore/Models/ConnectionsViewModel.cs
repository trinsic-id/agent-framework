using System.Collections.Generic;
using AgentFramework.Core.Models.Records;

namespace WebAgent.Models
{
    public class ConnectionsViewModel
    {
        public IEnumerable<ConnectionRecord> Connections { get; set; }

        public IEnumerable<ConnectionRecord> Invitations { get; set; }
    }
}
