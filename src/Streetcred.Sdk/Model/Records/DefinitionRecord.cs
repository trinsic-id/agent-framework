namespace Streetcred.Sdk.Model.Records
{
    /// <summary>
    /// Schema definition record.
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
        /// Gets or sets a value indicating whether this definition supports credential revocation.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this definition supports credential revocation; otherwise, <c>false</c>.
        /// </value>
        public bool SupportsRevocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether requests are automatically issued a credential.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [require approval]; otherwise, <c>false</c>.
        /// </value>
        public bool RequireApproval { get; set; }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "CredentialDefinition";
    }
}