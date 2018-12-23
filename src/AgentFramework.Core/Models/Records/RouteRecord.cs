using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Route record.
    /// </summary>
    public class RouteRecord : RecordBase
    {
        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string TypeName => "AF.Route";
        
        /// <summary>
        /// Gets or sets the connection id.
        /// </summary>
        /// <value>The connection id.</value>
        [JsonIgnore]
        public string ConnectionId
        {
            get => Get();
            set => Set(value);
        }
    }
}