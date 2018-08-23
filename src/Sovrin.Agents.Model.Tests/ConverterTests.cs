using System;
using Newtonsoft.Json;
using Sovrin.Agents.Model.Connections;
using Xunit;

namespace Sovrin.Agents.Model.Tests
{
    public class ConverterTests : IDisposable
    {
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
            var expected = new ForwardEnvelopeMessage { To = "123" };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IEnvelopeMessage>(json);

            Assert.IsType<ForwardEnvelopeMessage>(actual);
            Assert.Equal("123", ((ForwardEnvelopeMessage)actual).To);
        }

        [Fact]
        public void CanConvertContentMessage()
        {
            var expected = new ConnectionRequest { Key = "123" };
            var json = JsonConvert.SerializeObject(expected);

            var actual = JsonConvert.DeserializeObject<IContentMessage>(json);

            Assert.IsType<ConnectionRequest>(actual);
            Assert.Equal("123", ((ConnectionRequest)actual).Key);
        }

        public void Dispose()
        {
            
        }
    }
}
