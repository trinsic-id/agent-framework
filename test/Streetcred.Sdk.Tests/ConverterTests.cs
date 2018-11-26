using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Messages.Connections;
using Streetcred.Sdk.Models.Proofs;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class ConverterTests : IDisposable
    {
        [Fact]
        public void CanConvertToInvitation()
        {
            var expected = new ConnectionInvitationMessage { ConnectionKey = "123" };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IAgentMessage>(json);

            Assert.IsType<ConnectionInvitationMessage>(actual);
            Assert.Equal("123", ((ConnectionInvitationMessage) actual).ConnectionKey);
        }
        
        [Fact]
        public void CanConvertContentMessage()
        {
            var expected = new ConnectionRequestMessage
            {
                Did = "test-did"
            };

            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IAgentMessage>(json);

            Assert.IsType<ConnectionRequestMessage>(actual);
            Assert.Equal(MessageTypes.ConnectionRequest, ((ConnectionRequestMessage) actual).Type);
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