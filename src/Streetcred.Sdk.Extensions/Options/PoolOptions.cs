namespace Streetcred.Sdk.Extensions.Options
{
    public class PoolOptions
    {
        public string PoolName
        {
            get;
            set;
        } = "DefaultPool";

        public string GenesisFilename
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the protocol version of the nodes.
        /// </summary>
        /// <value>
        /// The protocol version.
        /// </value>
        public int ProtocolVersion
        {
            get;
            set;
        } = 2;
    }
}