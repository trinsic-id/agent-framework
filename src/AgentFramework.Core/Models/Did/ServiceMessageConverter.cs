using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Models.Did
{
    /// <summary>
    /// Message converter for serializing and deserializing content messages to and from json to their respective object types
    /// </summary>
    public class ServiceMessageConverter : JsonConverter
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteRawValue(JsonConvert.SerializeObject(value));

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var item = JObject.Load(reader);

            IDidService message;
            switch (item["Type"].ToString())
            {
                case DidServiceTypes.Agency:
                    message = new AgencyService();
                    break;
                default:
                    throw new TypeLoadException("Unsupported serialization type.");
            }

            serializer.Populate(item.CreateReader(), message);
            return message;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => true;
    }
}
