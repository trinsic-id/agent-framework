using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Runtime;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class MockAgentMessage : IAgentMessage
    {
        public string Id { get; set; }

        public string Type { get; set; }
    }

    public class MessageServiceTests : IAsyncLifetime
    {
        private string Config = "{\"id\":\"" + Guid.NewGuid() + "\"}";
        private const string WalletCredentials = "{\"key\":\"test_wallet_key\"}";

        private Wallet _wallet;

        private readonly IMessageService _messagingService;

        private readonly ConcurrentBag<HttpRequestMessage> _messages = new ConcurrentBag<HttpRequestMessage>();

        public MessageServiceTests()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                // Setup the PROTECTED method to mock
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // prepare the expected response of the mocked http call
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(""),
                })
                .Callback((HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    _messages.Add(request);
                })
                .Verifiable();

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object);

            var mockConnectionService = new Mock<IConnectionService>();
            mockConnectionService.Setup(_ => _.ListAsync(It.IsAny<IAgentContext>(), It.IsAny<ISearchQuery>(), It.IsAny<int>()))
                .Returns(Task.FromResult(new List<ConnectionRecord> {new ConnectionRecord()}));

            _messagingService =
                new DefaultMessageService(new Mock<ILogger<DefaultMessageService>>().Object, httpClient);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(Config, WalletCredentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            _wallet = await Wallet.OpenWalletAsync(Config, WalletCredentials);
        }

        public async Task DisposeAsync()
        {
            if (_wallet != null) await _wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(Config, WalletCredentials);
        }

        [Fact]
        public async Task PackAnon()
        {

            var message = new ConnectionInvitationMessage() {ConnectionKey = "123"}.ToByteArray();

            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var anotherMy = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            
            var packed = await CryptoUtils.PackAsync(_wallet, anotherMy.VerKey, null, message);

            Assert.NotNull(packed);
        }

        [Fact]
        public async Task PackAuth()
        {

            var message = new ConnectionInvitationMessage() {ConnectionKey = "123"}.ToByteArray();

            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var anotherMy = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            
            var packed = await CryptoUtils.PackAsync(_wallet, anotherMy.VerKey, my.VerKey, message);

            Assert.NotNull(packed);
        }

        [Fact]
        public async Task PackAndUnpackAnon()
        {

            var message = new ConnectionInvitationMessage() {ConnectionKey = "123"};

            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var anotherMy = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            
            var packed = await CryptoUtils.PackAsync(_wallet, anotherMy.VerKey, null, message);
            var unpack = await CryptoUtils.UnpackAsync(_wallet, packed);

            Assert.NotNull(unpack);
            Assert.Null(unpack.SenderVerkey);
            Assert.NotNull(unpack.RecipientVerkey);
            Assert.Equal(unpack.RecipientVerkey, anotherMy.VerKey);
        }

        [Fact]
        public async Task PackAndUnpackAuth()
        {

            var message = new ConnectionInvitationMessage() {ConnectionKey = "123"}.ToByteArray();

            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var anotherMy = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            
            var packed = await CryptoUtils.PackAsync(_wallet, anotherMy.VerKey, my.VerKey, message);
            var unpack = await CryptoUtils.UnpackAsync(_wallet, packed);

            var jObject = JObject.Parse(unpack.Message);

            Assert.NotNull(unpack);
            Assert.NotNull(unpack.SenderVerkey);
            Assert.NotNull(unpack.RecipientVerkey);
            Assert.Equal(unpack.RecipientVerkey, anotherMy.VerKey);
            Assert.Equal(unpack.SenderVerkey, my.VerKey);
            Assert.Equal(MessageTypes.ConnectionInvitation, jObject["@type"].ToObject<string>());
        }

        [Fact]
        public async Task UnpackToCustomType()
        {

            var message = new ConnectionInvitationMessage() {ConnectionKey = "123"};

            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var anotherMy = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            var packed = await CryptoUtils.PackAsync(_wallet, anotherMy.VerKey, null, message);
            var unpack = await CryptoUtils.UnpackAsync<ConnectionInvitationMessage>(_wallet, packed);

            Assert.NotNull(unpack);
            Assert.Equal("123", unpack.ConnectionKey);
        }
        

        [Fact]
        public async Task SendAsyncThrowsInvalidMessageNoId()
        {
            var connection = new ConnectionRecord
            {
                Alias = new ConnectionAlias
                {
                    Name = "Test"
                },
                Endpoint = new AgentEndpoint
                {
                    Uri = "https://mock.com"
                },
            };

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () =>
                await _messagingService.SendAsync(_wallet, new MockAgentMessage(), connection));
            Assert.True(ex.ErrorCode == ErrorCode.InvalidMessage);
        }

        [Fact]
        public async Task SendAsyncThrowsInvalidMessageNoType()
        {
            var connection = new ConnectionRecord
            {
                Alias = new ConnectionAlias
                {
                    Name = "Test"
                },
                Endpoint = new AgentEndpoint
                {
                    Uri = "https://mock.com"
                },
            };

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () =>
                await _messagingService.SendAsync(_wallet, new MockAgentMessage { Id = Guid.NewGuid().ToString() }, connection));
            Assert.True(ex.ErrorCode == ErrorCode.InvalidMessage);
        }
    }
}
