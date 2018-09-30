using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Proofs;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Model.Converters
{
    /// <summary>
    /// Message converter for serializing and deserializing content messages to and from json to their respective object types
    /// </summary>
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
            var (_, messageType) = MessageUtils.ParseMessageType(item["@type"].ToObject<string>());

            IContentMessage message;
            switch (messageType)
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