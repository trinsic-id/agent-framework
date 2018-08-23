using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sovrin.Agents.Model.Connections;
using Sovrin.Agents.Model.Credentials;
using Sovrin.Agents.Model.Proofs;

namespace Sovrin.Agents.Model.Converters
{
    public class ContentMessageConverter : JsonConverter
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

            IContentMessage message;
            switch (item["@type"].ToObject<string>())
            {
                case MessageTypes.ConnectionRequest:
                    message = new ConnectionRequest();
                    break;
                case MessageTypes.ConnectionResponse:
                    message = new ConnectionResponse();
                    break;
                case MessageTypes.CredentialOffer:
                    message = new CredentialOffer();
                    break;
                case MessageTypes.CredentialRequest:
                    message = new CredentialRequest();
                    break;
                case MessageTypes.Credential:
                    message = new Credential();
                    break;
                case MessageTypes.ProofRequest:
                    message = new ProofRequest();
                    break;
                case MessageTypes.DisclosedProof:
                    message = new Proof();
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