using System;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Connections;
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

        public void Dispose()
        {
            
        }
    }
}
