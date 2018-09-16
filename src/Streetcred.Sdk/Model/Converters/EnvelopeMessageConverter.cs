using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Model.Converters
{
    public class EnvelopeMessageConverter : JsonConverter
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
            var (_, messageType) = MessageUtils.ParseMessageType(item["@type"].ToObject<string>());

            IEnvelopeMessage message;
            switch (messageType)
            {
                case MessageTypes.Forward:
                    message = new ForwardEnvelopeMessage();
                    break;
                case MessageTypes.ForwardToKey:
                    message = new ForwardToKeyEnvelopeMessage();
                    break;
                default: throw new TypeLoadException("Unsupported serialization type.");
            }

            serializer.Populate(item.CreateReader(), message);
            return message;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => true;
    }
}