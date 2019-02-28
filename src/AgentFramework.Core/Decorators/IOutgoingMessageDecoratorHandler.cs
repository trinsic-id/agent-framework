using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Decorators
{
    /// <summary>
    /// Outgoing message decorator
    /// </summary>
    public interface IOutgoingMessageDecoratorHandler
    {
        /// <summary>
        /// The decorator identifier.
        /// </summary>
        string DecoratorIdentifier { get; }

        /// <summary>
        /// Processes the outgoing message.
        /// </summary>
        /// <param name="messageContext">The outgoing message.</param>
        /// <param name="wallet">The wallet.</param>
        /// <returns></returns>
        Task<OutgoingMessageContext> ProcessAsync(OutgoingMessageContext messageContext, Wallet wallet);
    }
}
