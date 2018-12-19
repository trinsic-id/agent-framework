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

        /// <inheritdoc />
        public override string Id => UniqueRecordId;

        /// <inheritdoc />
        public override string TypeName => "AF.ProvisioningRecord";

        /// <summary>
        /// Gets or sets the endpoint information for the provisioned agent.
        /// </summary>
        /// <returns>The endpoint information for the provisioned agent</returns>
        public AgentEndpoint Endpoint
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the owner information for the provisioned agent.
        /// </summary>
        /// <returns>The owner information for the provisioned agent</returns>
        public AgentOwner Owner
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
            internal set;
        }

        /// <summary>
        /// Gets or sets the issuer verkey for the provisioned agent.
        /// </summary>
        /// <returns>The issuer verkey for the provisioned agent</returns>
        public string IssuerVerkey
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the master key identifier for the provisioned agent.
        /// </summary>
        /// <returns>The master key identifier for the provisioned agent</returns>
        public string MasterSecretId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the tails base uri for the provisioned agent.
        /// </summary>
        /// <returns>The tails base uri for the provisioned agent</returns>
        public string TailsBaseUri
        {
            get;
            internal set;
        }
    }
}
