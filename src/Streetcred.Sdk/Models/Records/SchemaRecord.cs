namespace Streetcred.Sdk.Models.Records
{
    /// <summary>
    /// Schema record.
    /// </summary>
    public class SchemaRecord : WalletRecord
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public override string GetId() => SchemaId;

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "SchemaRecord";

        /// <summary>
        /// Gets or sets the schema identifier.
        /// </summary>
        /// <value>The schema identifier.</value>
        public string SchemaId { get; set; }

        /// <summary>
        /// Gets or sets the schema content as a json string.
        /// </summary>
        /// <value>The schema as a json string.</value>
        public string SchemaJson { get; set; }
    }
}
