using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework.Core.Models
{
    public class IssuerAgentConfiguration
    {
        public IssuerAgentConfiguration(string issuerSeed = null, Uri tailsBaseUri = null)
        {
            IssuerSeed = issuerSeed;
            TailsBaseUri = tailsBaseUri;
        }

        /// <summary>
        /// Gets or sets the tails service base URI.
        /// </summary>
        /// <value>The tails base URI.</value>
        public Uri TailsBaseUri { get; }

        /// <summary>
        /// Gets or sets the issuer seed used to generate deterministic DID and Verkey. (32 characters)
        /// <remarks>Leave <c>null</c> to generate random issuer did and verkey</remarks>
        /// </summary>
        /// <value>
        /// The issuer seed.
        /// </value>
        public string IssuerSeed { get; }
    }
}
