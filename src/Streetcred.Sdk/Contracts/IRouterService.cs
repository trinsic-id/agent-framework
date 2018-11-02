using System.Threading.Tasks;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Models;

namespace Streetcred.Sdk.Contracts
{
    /// <summary>
    /// Router service.
    /// </summary>
    public interface IRouterService
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
