using System.Collections.Generic;

namespace Streetcred.Sdk.Model.Connections
{
    /// <summary>
    /// Config for controlling invitation creation.
    /// </summary>
    public class DefaultCreateInviteConfiguration
    {
        /// <summary>
        /// Id of the resulting connection record created
        /// by the invite.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Alias object for marking the invite subject
        /// with an alias for giving the inviter greater context. 
        /// </summary>
        public ConnectionAlias TheirAlias { get; set; }

        /// <summary>
        /// For optionally setting my alias information
        /// on the invite.
        /// </summary>
        public ConnectionAlias MyAlias { get; set; }

        /// <summary>
        /// For automatically accepting a
        /// connection request generated from this created invite
        /// </summary>
        public bool AutoAcceptConnection { get; set; }

        /// <summary>
        /// Controls the tags that are persisted against the invite/connection record.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }
    }
}
