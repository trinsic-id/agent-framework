namespace AgentFramework.Core.Models.Did
{
    /// <summary>
    /// An object for containing agent endpoint information.
    /// </summary>
    public class AgentService : IDidService
    {
        public AgentService(AgencyService copy)
        {
            ServiceEndpoint = copy.ServiceEndpoint;
        }

        public AgentService() { }

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
        public string Type => DidServiceTypes.Agent;
        
        /// <summary>
        /// Gets or sets the uri of the agent.
        /// </summary>
        /// <value>
        /// The uri of the agent.
        /// </value>
        public string ServiceEndpoint { get; set; }
    }
}