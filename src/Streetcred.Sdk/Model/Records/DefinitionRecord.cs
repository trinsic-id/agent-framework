namespace Streetcred.Sdk.Model.Records
{
    /// <summary>
    /// Definition record.
    /// </summary>
    public class DefinitionRecord : WalletRecord
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public override string GetId() => DefinitionId;

        /// <summary>
        /// Gets or sets the definition identifier.
        /// </summary>
        /// <value>The definition identifier.</value>
        public string DefinitionId { get; set; }

        /// <summary>
        /// Gets or sets the definition json.
        /// </summary>
        /// <value>The definition json.</value>
        public bool Revocable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [require approval].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [require approval]; otherwise, <c>false</c>.
        /// </value>
        public bool RequireApproval { get; set; }

        /// <summary>
        /// Gets or sets the revocation registry identifier.
        /// </summary>
        /// <value>
        /// The revocation registry identifier.
        /// </value>
        public string RevocationRegistryId { get; set; }

        /// <summary>
        /// Gets or sets the tails storage identifier.
        /// </summary>
        /// <value>
        /// The tails storage identifier.
        /// </value>
        public string TailsStorageId { get; set; }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "CredentialDefinition";
    }
}
