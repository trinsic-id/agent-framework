namespace AgentFramework.Core.Exceptions
{
    public enum ErrorCode
    {
        /// <summary>
        /// The wallet already provisioned
        /// </summary>
        WalletAlreadyProvisioned,
        /// <summary>
        /// The record not found
        /// </summary>
        RecordNotFound,
        /// <summary>
        /// The record in invalid state
        /// </summary>
        RecordInInvalidState,
        /// <summary>
        /// The ledger operation rejected
        /// </summary>
        LedgerOperationRejected,
        /// <summary>
        /// The route message error
        /// </summary>
        RouteMessageError,
        /// <summary>
        /// The a2 a message transmission error
        /// </summary>
        A2AMessageTransmissionError,
        /// <summary>
        /// The invalid message
        /// </summary>
        InvalidMessage
    }
}
