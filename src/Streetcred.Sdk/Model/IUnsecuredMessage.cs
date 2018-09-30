using Newtonsoft.Json;
using Streetcred.Sdk.Model.Converters;

namespace Streetcred.Sdk.Model
{
    /// <summary>
    /// Represents an unsecured message
    /// </summary>
    [JsonConverter(typeof(UnsecuredMessageConverter))]
    public interface IUnsecuredMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        string Type { get; set; }
    }
}
