using Hyperledger.Indy.DidApi;

namespace AgentFramework.Core.Models.Connections
{
    /// <summary>
    /// Connection identity object for 
    /// </summary>
    public class ConnectionIdentity
    {
        public ConnectionIdentity(ConnectionIdentity copy)
        {
            Did = copy.Did;
            Verkey = copy.Verkey;
            ConnectionKey = copy.ConnectionKey;
        }

        public ConnectionIdentity(CreateAndStoreMyDidResult result, string connectionKey)
        {
            Did = result.Did;
            Verkey = result.VerKey;
            ConnectionKey = connectionKey;
        }

        public ConnectionIdentity(CreateAndStoreMyDidResult result)
        {
            Did = result.Did;
            Verkey = result.VerKey;
        }

        public ConnectionIdentity() { }

        /// <summary>
        /// Connection key to use in an invitation
        /// </summary>
        public string ConnectionKey { get; }

        /// <summary>
        /// Did used for identification
        /// </summary>
        public string Did { get; }

        /// <summary>
        /// Verkey used for identification
        /// </summary>
        public string Verkey { get; }
    }
}
