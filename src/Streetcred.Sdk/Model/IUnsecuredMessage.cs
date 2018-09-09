using Newtonsoft.Json;
using Streetcred.Sdk.Model.Converters;

namespace Streetcred.Sdk.Model
{
    [JsonConverter(typeof(UnsecuredMessageConverter))]
    public interface IUnsecuredMessage
    {
        string Type { get; set; }
    }
}
