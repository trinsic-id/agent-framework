using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Messages.Connections;
using Streetcred.Sdk.Messages.Credentials;
using Streetcred.Sdk.Messages.Proofs;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Messages
{
    /// <summary>
    /// Message converter for serializing and deserializing content messages to and from json to their respective object types
    /// </summary>
    public class AgentMessageConverter : JsonConverter
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
            
            IAgentMessage message;
            switch (item["@type"].ToString())
            {
                case MessageTypes.ConnectionInvitation:
                    message = new ConnectionInvitationMessage();
                    break;
                case MessageTypes.ConnectionRequest:
                    message = new ConnectionRequestMessage();
                    break;
                case MessageTypes.ConnectionResponse:
                    message = new ConnectionResponseMessage();
                    break;
                case MessageTypes.CredentialOffer:
                    message = new CredentialOfferMessage();
                    break;
                case MessageTypes.CredentialRequest:
                    message = new CredentialRequestMessage();
                    break;
                case MessageTypes.Credential:
                    message = new CredentialMessage();
                    break;
                case MessageTypes.ProofRequest:
                    message = new ProofRequestMessage();
                    break;
                case MessageTypes.DisclosedProof:
                    message = new ProofMessage();
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