using Streetcred.Sdk.Model.Connections;

namespace Streetcred.Sdk.Model.Records
{
    public class ProvisioningRecord : WalletRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProvisioningRecord"/> class.
        /// </summary>
        public ProvisioningRecord()
        {
            Endpoint = new AgentEndpoint();
            Owner = new AgentOwner();
        }

        internal const string RecordId = "SingleRecord";

        public override string GetId() => RecordId;

        public override string GetTypeName() => "ProvisioningRecord";

        public AgentEndpoint Endpoint { get; set; }

        public AgentOwner Owner { get; set; }

        public string AgentSeed { get; set; }

        public string IssuerSeed { get; set; }

        public string IssuerDid { get; set; }

        public string IssuerVerkey { get; set; }

        public string MasterSecretId { get; set; }

        public string TailsBaseUri { get; set; }

        /// <summary>
        /// Determines whether this wallet is provisioned as issuer.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is issuer; otherwise, <c>false</c>.
        /// </returns>
        public bool IsIssuer() => !string.IsNullOrEmpty(IssuerDid);
    }
}
