using System;
using Newtonsoft.Json;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class ConverterTests : IDisposable
    {
        [Fact]
        public void CanConvertToInvitation()
        {
            var expected = new ConnectionInvitation { ConnectionKey = "123" };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IUnsecuredMessage>(json);

            Assert.IsType<ConnectionInvitation>(actual);
            Assert.Equal("123", ((ConnectionInvitation)actual).ConnectionKey);
        }

        [Fact]
        public void CanConvertUnsecuredMessage()
        {
            var expected = new ConnectionInvitation { ConnectionKey = "123" };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IUnsecuredMessage>(json);

            Assert.IsType<ConnectionInvitation>(actual);
            Assert.Equal("123", ((ConnectionInvitation)actual).ConnectionKey);
        }

        [Fact]
        public void CanConvertEnvelopeMessage()
        {
            var type = MessageUtils.FormatDidMessageType("3NnbYBdhyHfuFZnbaZhuU6", MessageTypes.ConnectionRequest);

            var expected = new ForwardEnvelopeMessage { Type = type };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IEnvelopeMessage>(json);

            Assert.IsType<ForwardEnvelopeMessage>(actual);
            Assert.Equal(type, ((ForwardEnvelopeMessage)actual).Type);
        }

        [Fact]
        public void CanConvertContentMessage()
        {
            var type = MessageUtils.FormatKeyMessageType("2J6h65V5CjvWceHDMq7htRkG6EdCE2SiDEtCRyfngwfw", MessageTypes.ConnectionRequest);

            var expected = new ConnectionRequest
            {
                Key = "2J6h65V5CjvWceHDMq7htRkG6EdCE2SiDEtCRyfngwfw",
                Type = type
            };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IContentMessage>(json);

            Assert.IsType<ConnectionRequest>(actual);
            Assert.Equal(type, ((ConnectionRequest)actual).Type);
        }

        public void Dispose()
        {

        }
    }
}
