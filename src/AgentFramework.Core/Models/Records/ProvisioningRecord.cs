namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents a provisioning record in the agency wallet
    /// </summary>
    /// <seealso cref="RecordBase" />
    public class ProvisioningRecord : RecordBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProvisioningRecord"/> class.
        /// </summary>
        public ProvisioningRecord()
        {
            Endpoint = new AgentEndpoint();
            Owner = new AgentOwner();
        }

        /// <summary>
        /// Record Identifier
        /// </summary>
        internal const string UniqueRecordId = "SingleRecord";

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public override string Id => UniqueRecordId;

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string TypeName => "AF.ProvisioningRecord";

        /// <summary>
        /// Gets or sets the endpoint information for the provisioned agent.
        /// </summary>
        /// <returns>The endpoint informtation for the provisioned agent</returns>
        public AgentEndpoint Endpoint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the owner information for the provisioned agent.
        /// </summary>
        /// <returns>The owner informtation for the provisioned agent</returns>
        public AgentOwner Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the seed used to generate the did for the provisioned agent.
        /// </summary>
        /// <returns>The seed used to generate the did for the provisioned agent</returns>
        public string AgentSeed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the seed used to generate the issuer did for the provisioned agent.
        /// </summary>
        /// <returns>The seed used to generate the issuer did for the provisioned agent</returns>
        public string IssuerSeed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the issuer did for the provisioned agent.
        /// </summary>
        /// <returns>The issuer did for the provisioned agent</returns>
        public string IssuerDid
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the issuer verkey for the provisioned agent.
        /// </summary>
        /// <returns>The issuer verkey for the provisioned agent</returns>
        public string IssuerVerkey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the master key identifier for the provisioned agent.
        /// </summary>
        /// <returns>The master key identifier for the provisioned agent</returns>
        public string MasterSecretId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tails base uri for the provisioned agent.
        /// </summary>
        /// <returns>The tails base uri for the provisioned agent</returns>
        public string TailsBaseUri
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether this wallet is provisioned as issuer.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is issuer; otherwise, <c>false</c>.
        /// </returns>
        public bool IsIssuer() => !string.IsNullOrEmpty(IssuerDid);
    }
}
