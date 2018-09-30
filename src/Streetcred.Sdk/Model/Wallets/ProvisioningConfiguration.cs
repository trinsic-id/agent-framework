using System;

namespace Streetcred.Sdk.Model.Wallets
{
    /// <summary>
    /// A configuration object for controlling the provisioning of a new agent.
    /// </summary>
    public class ProvisioningConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the owner of the agent
        /// </summary>
        /// <value>
        /// The agent owner name 
        /// </value>
        public string OwnerName { get; set; }

        /// <summary>
        /// Gets or sets the imageUrl of the owner of the agent
        /// </summary>
        /// <value>
        /// The agent owner image url
        /// </value>
        public string OwnerImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the agent seed used to generate deterministic DID and Verkey. (32 characters)
        /// <remarks>Leave <c>null</c> to generate random agent did and verkey</remarks>
        /// </summary>
        /// <value>
        /// The agent seed.
        /// </value>
        public string AgentSeed { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URI that this agent will receive Sovrin messages
        /// </summary>
        /// <value>
        /// The endpoint URI.
        /// </value>
        public Uri EndpointUri { get; set; }

        /// <summary>
        /// Gets or sets the issuer seed used to generate deterministic DID and Verkey. (32 characters)
        /// <remarks>Leave <c>null</c> to generate random issuer did and verkey</remarks>
        /// </summary>
        /// <value>
        /// The issuer seed.
        /// </value>
        public string IssuerSeed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an issuer did and verkey should be generated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [create issuer]; otherwise, <c>false</c>.
        /// </value>
        public bool CreateIssuer { get; set; }

        /// <summary>
        /// Gets or sets the tails service base URI.
        /// </summary>
        /// <value>The tails base URI.</value>
        public string TailsBaseUri { get; set; }
    }
}
