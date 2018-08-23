using Newtonsoft.Json;
using Sovrin.Agents.Model.Converters;

namespace Sovrin.Agents.Model
{
    [JsonConverter(typeof(UnsecuredMessageConverter))]
    public interface IUnsecuredMessage
    {
        string Type { get; set; }
    }
}
