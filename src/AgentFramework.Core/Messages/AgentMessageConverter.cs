using System;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Messages.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Messages
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
                case MessageTypes.Forward:
                    message = new ForwardMessage();
                    break;
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
                case MessageTypes.ForwardMultiple:
                    message = new ForwardMultipleMessage();
                    break;
                case MessageTypes.Forward:
                    message = new ForwardMessage();
                    break;
                case MessageTypes.CreateRoute:
                    message = new CreateRouteMessage();
                    break;
                case MessageTypes.DeleteRoute:
                    message = new DeleteRouteMessage();
                    break;
                case MessageTypes.GetRoutes:
                    message = new GetRoutesMessage();
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