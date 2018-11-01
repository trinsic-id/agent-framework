using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Messages.Connection;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Wallets;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class ConverterTests : IDisposable
    {
        [Fact]
        public void CanConvertToInvitation()
        {
            var expected = new ConnectionInvitation {ConnectionKey = "123"};
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IUnsecuredMessage>(json);

            Assert.IsType<ConnectionInvitation>(actual);
            Assert.Equal("123", ((ConnectionInvitation) actual).ConnectionKey);
        }

        [Fact]
        public void CanConvertUnsecuredMessage()
        {
            var expected = new ConnectionInvitation {ConnectionKey = "123"};
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IUnsecuredMessage>(json);

            Assert.IsType<ConnectionInvitation>(actual);
            Assert.Equal("123", ((ConnectionInvitation) actual).ConnectionKey);
        }

        [Fact]
        public void CanConvertEnvelopeMessage()
        {
            var type = MessageUtils.FormatDidMessageType("3NnbYBdhyHfuFZnbaZhuU6", MessageTypes.Forward);

            var expected = new ForwardEnvelopeMessage {Type = type};
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IEnvelopeMessage>(json);

            Assert.IsType<ForwardEnvelopeMessage>(actual);
            Assert.Equal(type, ((ForwardEnvelopeMessage) actual).Type);
        }

        [Fact]
        public void CanConvertContentMessage()
        {
            var type = MessageUtils.FormatKeyMessageType("2J6h65V5CjvWceHDMq7htRkG6EdCE2SiDEtCRyfngwfw",
                MessageTypes.ConnectionRequest);

            var expected = new ConnectionRequest
            {
                Key = "2J6h65V5CjvWceHDMq7htRkG6EdCE2SiDEtCRyfngwfw",
                Type = type
            };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IContentMessage>(json);

            Assert.IsType<ConnectionRequest>(actual);
            Assert.Equal(type, ((ConnectionRequest) actual).Type);
        }

        [Fact]
        public void EnumAttributeSerializesToJson()
        {
            var attributeFilter = new Dictionary<AttributeFilter, string>
            {
                {AttributeFilter.CredentialDefinitionId, "123"}
            };

            var json = attributeFilter.ToJson();

            var jobj = JObject.Parse(json);

            Assert.True(jobj.ContainsKey("cred_def_id"));
            Assert.Equal("123", jobj.GetValue("cred_def_id"));
        }

        [Fact]
        public void EnumAttributeDeserializesFromJson()
        {
            var attributeFilter = new Dictionary<AttributeFilter, string>
            {
                {AttributeFilter.SchemaIssuerDid, "123"}
            };

            var json = attributeFilter.ToJson();

            var dict = JsonConvert.DeserializeObject<Dictionary<AttributeFilter, string>>(json);

            Assert.True(dict.ContainsKey(AttributeFilter.SchemaIssuerDid));
            Assert.Equal("123", dict[AttributeFilter.SchemaIssuerDid]);
        }

        public void Dispose()
        {

        }
    }
}