using AgentFramework.Core.Models.Did;

namespace AgentFramework.Core.Models
{
    /// <summary>
    /// An object for containing agent endpoint information.
    /// </summary>
    public class AgencyService : IDidService
    {
        public AgencyService(AgencyService copy)
        {
            Verkey = copy.Verkey;
            ServiceEndpoint = copy.ServiceEndpoint;
        }

        public AgencyService() { }

        /// <summary>
        /// Gets or sets the identifier of the service.
        /// </summary>
        /// <value>
        /// The identifier of the service.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public string Type => DidServiceTypes.Agency;
        
        /// <summary>
        /// Gets or sets the verkey of the agent.
        /// </summary>
        /// <value>
        /// The verkey of the agent.
        /// </value>
        public string Verkey { get; set; }

        /// <summary>
        /// Gets or sets the uri of the agent.
        /// </summary>
        /// <value>
        /// The uri of the agent.
        /// </value>
        public string ServiceEndpoint { get; set; }
    }
}