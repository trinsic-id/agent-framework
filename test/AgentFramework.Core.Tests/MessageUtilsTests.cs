using System;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Utils;
using Newtonsoft.Json;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class MessageUtilsTests
    {
        [Fact]
        public void CanEncodeMessageToUrl()
        {
            var message = new ConnectionInvitationMessage();
            var exampleUrl = "http://example.com";

            var encodedMessage = MessageUtils.EncodeMessageToUrlFormat(exampleUrl, message);

            Uri.IsWellFormedUriString(encodedMessage, UriKind.Absolute);
        }

        [Fact]
        public void EncodeMessageToUrlThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MessageUtils.EncodeMessageToUrlFormat((string)null, new ConnectionInvitationMessage()));
            Assert.Throws<ArgumentNullException>(() => MessageUtils.EncodeMessageToUrlFormat((Uri)null, new ConnectionInvitationMessage()));
            Assert.Throws<ArgumentNullException>(() => MessageUtils.EncodeMessageToUrlFormat("", new ConnectionInvitationMessage()));
            Assert.Throws<ArgumentNullException>(() => MessageUtils.EncodeMessageToUrlFormat(new Uri("http://example.com"), (ConnectionInvitationMessage)null));
        }

        [Fact]
        public void DecodeMessageToUrlThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MessageUtils.DecodeMessageFromUrlFormat(null));
            Assert.Throws<ArgumentNullException>(() => MessageUtils.DecodeMessageFromUrlFormat(""));
        }

        [Fact]
        public void CanDecodeMessageFromUrl()
        {
            var urlEncodedMessage =
                "http://example.com/?m=eyJsYWJlbCI6bnVsbCwiaW1hZ2VVcmwiOm51bGwsInNlcnZpY2VFbmRwb2ludCI6bnVsbCwicm91dGluZ0tleXMiOm51bGwsInJlY2lwaWVudEtleXMiOm51bGwsIkBpZCI6IjY5NTg5ODY2LTgxMzItNDM4Mi1iZDVmLThjNDFjZmEyOGFhMyIsIkB0eXBlIjoiZGlkOnNvdjpCekNic05ZaE1yakhpcVpEVFVBU0hnO3NwZWMvY29ubmVjdGlvbnMvMS4wL2ludml0YXRpb24ifQ";

            var jsonMessage = MessageUtils.DecodeMessageFromUrlFormat(urlEncodedMessage);

            var message = JsonConvert.DeserializeObject<ConnectionRequestMessage>(jsonMessage);
        }
    }
}