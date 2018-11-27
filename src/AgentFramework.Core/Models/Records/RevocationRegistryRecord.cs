namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents revocation registry record.
    /// </summary>
    public class RevocationRegistryRecord : WalletRecord
    {
        /// <summary>
        /// Gets the identifier of this <see cref="RevocationRegistryRecord"/>.
        /// </summary>
        /// <returns>The identifier.</returns>
        public override string GetId() => RevocationRegistryId;

        /// <summary>
        /// Gets or sets the revocation registry identifier.
        /// </summary>
        /// <value>The revocation registry identifier.</value>
        public string RevocationRegistryId { get; set; }

        /// <summary>
        /// Gets or sets the tails file where the registry data is stored.
        /// </summary>
        /// <value>The tails file.</value>
        public string TailsFile { get; set; }

        /// <summary>
        /// Gets the name of the record type for this object.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "RevocationRegistryRecord";
    }
}
