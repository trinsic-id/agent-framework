using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Messages
{
    internal class AgentMessageConverter<T> : JsonConverter where T : AgentMessage, new ()
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var val = JObject.FromObject(value);
            var message = (T)value;

            var decorators = message.GetDecorators();

            foreach (var decorator in decorators)
                val.Add(decorator.Name, decorator.Value);

            writer.WriteRawValue(val.ToString());
        }

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var item = JObject.Load(reader);

            var decorators = item.Properties().Where(_ => _.Name.StartsWith("~"));

            var obj = new T();
            obj.SetDecorators(decorators.ToList());

            serializer.Populate(item.CreateReader(), obj);
            return obj;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => true;
    }
}
