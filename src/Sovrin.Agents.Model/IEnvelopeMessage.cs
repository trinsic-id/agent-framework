using Newtonsoft.Json;
using Sovrin.Agents.Model.Converters;

namespace Sovrin.Agents.Model
{
    [JsonConverter(typeof(EnvelopeMessageConverter))]
    public interface IEnvelopeMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        string Type { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        string Content { get; set; }
    }
}