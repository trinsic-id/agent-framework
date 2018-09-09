namespace Streetcred.Sdk.Model.Credentials
{
    public class CredentialDetails
    {
        /// <summary>
        /// Gets or sets the credential json.
        /// </summary>
        /// <value>
        /// The credential json.
        /// </value>
        public string CredentialJson { get; set; }


        /// <summary>
        /// Gets or sets the revocation registry identifier.
        /// </summary>
        /// <value>
        /// The revocation registry identifier.
        /// </value>
        public string RevocationRegistryId { get; set; }
    }
}