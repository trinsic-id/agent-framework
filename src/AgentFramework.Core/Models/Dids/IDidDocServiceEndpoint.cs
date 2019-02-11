namespace AgentFramework.Core.Models.Dids
{
    /// <summary>
    /// DID doc service interface.
    /// </summary>
    public interface IDidDocServiceEndpoint
    {
        /// <summary>
        /// Id of the service.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Type of the service.
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Endpoint of the service.
        /// </summary>
        string ServiceEndpoint { get; set; }
    }
}