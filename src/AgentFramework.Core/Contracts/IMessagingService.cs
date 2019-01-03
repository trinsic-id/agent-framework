using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Messaging service.
    /// </summary>
    public interface IMessagingService
    {
        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, IAgentMessage message, ConnectionRecord connection, string recipientKey = null);

        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.A2AMessageTransmissionError.</exception>
        /// <returns>The response async.</returns>
        Task SendAsync(Wallet wallet, byte[] message, ConnectionRecord connection,
            string recipientKey = null);

        /// <summary>
        /// Packs a forward message for the supplied message and recipients
        /// </summary>
        /// <param name="verkey">Verkey of the intermidiate recipient.</param>
        /// <param name="message">Encrypted message for the recipients.</param>
        /// <param name="recipientIdentifier">Recipient identifiers of the inner forward message.</param>
        /// <returns></returns>
        Task<byte[]> PackForwardMessage(string verkey, byte[] message, string recipientIdentifier);
    }
}
