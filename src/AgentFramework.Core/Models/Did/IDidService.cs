
using AgentFramework.Core.Messages;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Did
{
    [JsonConverter(typeof(ServiceMessageConverter))]
    public interface IDidService
    {
        /// <summary>
        /// Gets or sets the identifier of the service.
        /// </summary>
        /// <value>
        /// The identifier of the service.
        /// </value>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        string Type { get; }
        
        /// <summary>
        /// Gets or sets the uri of the agent.
        /// </summary>
        /// <value>
        /// The uri of the agent.
        /// </value>
        string ServiceEndpoint { get; set; }
    }
}
