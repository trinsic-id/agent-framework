using System;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
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
                "http://127.0.1.1:8080/indy?c_i=eyJAdHlwZSI6ICJkaWQ6c292OkJ6Q2JzTlloTXJqSGlxWkRUVUFTSGc7c3BlYy9jb25uZWN0aW9ucy8xLjAvaW52aXRhdGlvbiIsICJsYWJlbCI6ICJNb250eSIsICJpbWFnZVVybCI6ICJodHRwczovL3Bicy50d2ltZy5jb20vcHJvZmlsZV9pbWFnZXMvNjk3MDM1MzgzNjc5Mjk1NDg4L182dmw3NHRNXzQwMHg0MDAucG5nIiwgInJlY2lwaWVudEtleXMiOiBbInljVW80QzdCRUxLcG9OaVA4ZDgxQ254cG1TU0J3alREZUNaTEpETkNUR0ciXSwgInNlcnZpY2VFbmRwb2ludCI6ICJodHRwOi8vMTI3LjAuMS4xOjgwODAvaW5keSIsICJyb3V0aW5nS2V5cyI6IFtdLCAiQGlkIjogIjA1YjNjMjkxLWE1YWEtNGJjOS1iZGU5LTdiMzNkMWRmZDMwMCJ9";

            var message = MessageUtils.DecodeMessageFromUrlFormat<ConnectionRequestMessage>(urlEncodedMessage);

            Assert.NotNull(message);
        }

        [Fact]
        public void DecodeMessageTypeUriThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MessageUtils.DecodeMessageTypeUri(null));
            Assert.Throws<ArgumentNullException>(() => MessageUtils.DecodeMessageTypeUri(""));
        }

        [Fact]
        public void DecodeMessageTypeUriThrowsInvalidParameterException()
        {
            var ex = Assert.Throws<AgentFrameworkException>(() => MessageUtils.DecodeMessageTypeUri("did:sov:123456789abcdefghi1234;spec"));
            Assert.True(ex.ErrorCode == ErrorCode.InvalidParameterFormat);
        }

        [Fact]
        public void CanDecodeValidMessageTypeUri()
        {
            var validMessageTypeUri = "did:sov:123456789abcdefghi1234;spec/examplefamily/1.0/exampletype";

            var (uri, messageFamilyName, messageVersion, messageName) = MessageUtils.DecodeMessageTypeUri(validMessageTypeUri);

            Assert.True(uri == "did:sov:123456789abcdefghi1234;spec");
            Assert.True(messageFamilyName == "examplefamily");
            Assert.True(messageVersion == "1.0");
            Assert.True(messageName == "exampletype");
        }

        [Fact]
        public void CanCreateMessageTypeObjFromUri()
        {
            var validMessageTypeUri = "did:sov:123456789abcdefghi1234;spec/examplefamily/1.0/exampletype";

            var messageType = new MessageType(validMessageTypeUri);

            Assert.True(messageType.BaseUri == "did:sov:123456789abcdefghi1234;spec");
            Assert.True(messageType.MessageFamilyName == "examplefamily");
            Assert.True(messageType.MessageVersion == "1.0");
            Assert.True(messageType.MessageName == "exampletype");
        }
    }
}