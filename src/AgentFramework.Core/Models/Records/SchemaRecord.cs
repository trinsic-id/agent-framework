namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Schema record.
    /// </summary>
    public class SchemaRecord : RecordBase
    {
        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string TypeName => "AF.SchemaRecord";

        /// <summary>
        /// Gets or sets the schema content as a json string.
        /// </summary>
        /// <value>The schema as a json string.</value>
        public string SchemaJson
        {
            get => Get();
            set => Set(value);
        }
    }
}