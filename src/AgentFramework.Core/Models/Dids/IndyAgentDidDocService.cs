using System.Collections.Generic;

namespace AgentFramework.Core.Models.Dids
{
    /// <summary>
    /// Indy Agent Did Doc Service.
    /// </summary>
    public class IndyAgentDidDocService : IDidDocServiceEndpoint
    {
        /// <inheritdoc />
        public string Id { get; set; }

        /// <inheritdoc />
        public string Type => "IndyAgent";
        
        /// <summary>
        /// Array of recipient key references.
        /// </summary>
        public IList<string> RecipientKeys { get; set; }

        /// <summary>
        /// Array or routing key references.
        /// </summary>
        public IList<string> RoutingKeys { get; set; }

        /// <summary>
        /// Service endpoint.
        /// </summary>
        public string ServiceEndpoint { get; set; }
    }
}
