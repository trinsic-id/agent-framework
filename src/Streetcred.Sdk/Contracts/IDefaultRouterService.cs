using System.Threading.Tasks;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Router service.
    /// </summary>
    public interface IDefaultRouterService
    {
        /// <summary>
        /// Forwards the asynchronous.
        /// </summary>
        /// <param name="envelope">The content.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        Task ForwardAsync(IEnvelopeMessage envelope, AgentEndpoint endpoint);
    }
}
