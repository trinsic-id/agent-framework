namespace Streetcred.Sdk.Model.Connections
{
    /// <summary>
    /// Config for controlling invitation creation
    /// </summary>
    public class CreateInviteConfiguration
    {
        /// <summary>
        /// Id of the resulting connection record created
        /// by the invite
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Alias object for marking the invite subject
        /// with an alias for giving the inviter greater context 
        /// </summary>
        public ConnectionAlias TheirAlias { get; set; }

        /// <summary>
        /// For optionally setting my alias information
        /// on the invite
        /// </summary>
        public ConnectionAlias MyAlias { get; set; }

        /// <summary>
        /// For automatically accepting a
        /// connection request generated from this created invite
        /// on the invite
        /// </summary>
        public bool AutoAcceptConnection { get; set; }
    }
}
