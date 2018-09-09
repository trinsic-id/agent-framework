using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Model.Connections;

namespace Streetcred.Sdk.Model.Converters
{
    public class UnsecuredMessageConverter : JsonConverter
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
            IUnsecuredMessage message;

            var item = JObject.Load(reader);
            switch (item["@type"].ToObject<string>())
            {
                case MessageTypes.ConnectionInvitation:
                    message = new ConnectionInvitation();
                    break;
                default: throw new TypeLoadException("Unsupported serialization type.");
            }

            serializer.Populate(item.CreateReader(), message);
            return message;
        }

        public override bool CanConvert(Type objectType) => true;
    }
}
