namespace Streetcred.Sdk.Model.Credentials
{
    /// <summary>
    /// Inner details object for a credential request content message.
    /// </summary>
    public class CredentialRequestDetails
    {
        /// <summary>
        /// Gets or sets the offer json.
        /// </summary>
        /// <value>
        /// The offer json.
        /// </value>
        public string OfferJson { get; set; }

        /// <summary>
        /// Gets or sets the credential request json.
        /// </summary>
        /// <value>
        /// The credential request json.
        /// </value>
        public string CredentialRequestJson { get; set; }

        /// <summary>
        /// Gets or sets the credential values json.
        /// </summary>
        /// <value>
        /// The credential values json.
        /// </value>
        public string CredentialValuesJson { get; set; }
    }
}