using System.Collections.Generic;
using Streetcred.Sdk.Model.Records;

namespace WebAgent.Models
{
    public class ConnectionsViewModel
    {
        public IEnumerable<ConnectionRecord> Connections { get; set; }

        public IEnumerable<ConnectionRecord> Invitations { get; set; }
    }
}
