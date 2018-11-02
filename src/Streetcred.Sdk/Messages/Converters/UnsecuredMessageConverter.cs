using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Messages.Connections;

namespace Streetcred.Sdk.Messages.Converters
{
    /// <summary>
    /// Message converter for serializing and deserializing unsecured messages to and from json to their respective object types
    /// </summary>
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
                    message = new ConnectionInvitationMessage();
                    break;
                default: throw new TypeLoadException("Unsupported serialization type.");
            }

            serializer.Populate(item.CreateReader(), message);
            return message;
        }

        public override bool CanConvert(Type objectType) => true;
    }
}
