namespace Streetcred.Sdk.Model.Proofs
{
    /// <summary>
    /// Inner details object for a proof content message.
    /// </summary>
    public class ProofDetails
    {
        /// <summary>
        /// Gets or sets the proof json.
        /// </summary>
        /// <value>
        /// The proof json.
        /// </value>
        public string ProofJson { get; set; }

        /// <summary>
        /// Gets or sets the proof request nonce.
        /// </summary>
        /// <value>
        /// The request nonce.
        /// </value>
        public string RequestNonce { get; set; }
    }
}