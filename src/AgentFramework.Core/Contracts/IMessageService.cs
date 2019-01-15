using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Messaging;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Router service.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Sends the agent message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null);

        /// <summary>
        /// Recieves an agent message asynchronously constructing a context object.
        /// </summary>
        /// <param name="agentContext">The agent context.</param>
        /// <param name="message">The message.</param>
        /// <returns>The message context asynchronously.</returns>
        Task<MessageContext> RecieveAsync(AgentContext agentContext, byte[] message);
    }
}
