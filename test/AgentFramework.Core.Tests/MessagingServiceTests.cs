using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class MessagingServiceTests : IAsyncLifetime
    {
        private string Config = "{\"id\":\"" + Guid.NewGuid() + "\"}";
        private const string WalletCredentials = "{\"key\":\"test_wallet_key\"}";

        private Wallet _wallet;

        private readonly IMessagingService _messagingService;
        private readonly IMessageSerializer _messageSerializer;

        private readonly ConcurrentBag<HttpRequestMessage> _messages = new ConcurrentBag<HttpRequestMessage>();

        public MessagingServiceTests()
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

            _messageSerializer = new DefaultMessageSerializer();

            _messagingService = new DefaultMessagingService(_messageSerializer,
                new Mock<ILogger<DefaultMessagingService>>().Object, httpClient);
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
        public async Task SendAsyncThrowsConnectionInvalidState()
        {
            var connectionRecord = new ConnectionRecord
            {
                State = ConnectionState.Invited
            };

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _messagingService.SendAsync(_wallet, new ConnectionRequestMessage(), connectionRecord, "test"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task SendAsyncThrowsProvisionRecordInvalidStateNoDidServices()
        {
            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var their = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            var connectionRecord = new ConnectionRecord
            {
                State = ConnectionState.Connected,
                MyVk = my.VerKey,
                MyDid = my.Did,
                TheirDid = their.Did,
                TheirVk = their.VerKey
            };

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _messagingService.SendAsync(_wallet, new ConnectionRequestMessage(), connectionRecord));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task CanSendMessageToAgentService()
        {
            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var their = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            var myConnection = new ConnectionRecord
            {
                State = ConnectionState.Connected,
                MyDid = my.Did,
                MyVk = my.VerKey,
                Alias = new ConnectionAlias
                {
                    Name = "Test"
                },
                TheirDid = their.Did,
                TheirVk = their.VerKey
            };

            myConnection.Services.Add(new AgentService
            {
                ServiceEndpoint = "https://mock.com"
            });

            await _messagingService.SendAsync(_wallet, new ConnectionRequestMessage(), myConnection);

            var httpMessage = _messages.FirstOrDefault();

            Assert.True(httpMessage != null);

            Assert.True(httpMessage.Method == HttpMethod.Post);
            Assert.True(httpMessage.RequestUri == new Uri("https://mock.com"));
            Assert.True(httpMessage.Content.Headers.ContentType.ToString() == DefaultMessagingService.AgentWireMessageMimeType);

            byte[] body = await httpMessage.Content.ReadAsByteArrayAsync();

            (var message, var theirKey, var to) =
                await _messageSerializer.AuthUnpackAsync(body, _wallet);

            Assert.True(message.Type == MessageTypes.ConnectionRequest);
            Assert.True(their.VerKey == theirKey);
            Assert.True(my.VerKey == to);
        }

        [Fact]
        public async Task CanSendMessageToAgencyService()
        {
            var my = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var their = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");
            var agency = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            var myConnection = new ConnectionRecord
            {
                State = ConnectionState.Connected,
                MyDid = my.Did,
                MyVk = my.VerKey,
                Alias = new ConnectionAlias
                {
                    Name = "Test"
                },
                TheirDid = their.Did,
                TheirVk = their.VerKey
            };

            myConnection.Services.Add(new AgencyService
            {
                ServiceEndpoint = "https://mock.com",
                Verkey = agency.VerKey
            });

            await _messagingService.SendAsync(_wallet, new ConnectionRequestMessage(), myConnection);

            var httpMessage = _messages.FirstOrDefault();

            Assert.True(httpMessage != null);

            Assert.True(httpMessage.Method == HttpMethod.Post);
            Assert.True(httpMessage.RequestUri == new Uri("https://mock.com"));
            Assert.True(httpMessage.Content.Headers.ContentType.ToString() == DefaultMessagingService.AgentWireMessageMimeType);

            byte[] body = await httpMessage.Content.ReadAsByteArrayAsync();

            var outerMessage = await _messageSerializer.AnonUnpackAsync(body, _wallet);

            Assert.IsType<ForwardMessage>(outerMessage);

            var forwardMessage = outerMessage as ForwardMessage;

            var innerMessageContents = Convert.FromBase64String(forwardMessage.Message);

            (var message, var theirKey, var to) =
                await _messageSerializer.AuthUnpackAsync(innerMessageContents, _wallet);

            Assert.True(message.Type == MessageTypes.ConnectionRequest);
            Assert.True(their.VerKey == theirKey);
            Assert.True(my.VerKey == to);
        }
    }
}
