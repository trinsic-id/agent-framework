using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class MockAgentHttpHandler : HttpMessageHandler
    {
        public MockAgentHttpHandler(Action<byte[]> callback)
        {
            Callback = callback;
        }

        public Action<byte[]> Callback { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
            {
                throw new Exception("Invalid http method");
            }
            Callback(await request.Content.ReadAsByteArrayAsync());
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    public class MockAgent : AgentBase
    {
        public MockAgent(
            IServiceProvider provider, 
            IConnectionService connectionService, 
            ILogger<AgentBase> logger) : base(provider, connectionService, logger)
        {
        }

        internal Task HandleAsync(byte[] data, Wallet wallet, Pool pool = null) => ProcessAsync(data, wallet, pool);
    }

    public class ConnectionTests : IAsyncLifetime
    {
        private readonly string _issuerConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}"; 
        private readonly string _holderConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private readonly string _holderConfigTwo = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";
        
        private readonly Mock<IProvisioningService> _provisioningMock;

        private IAgentContext _issuerWallet;
        private IAgentContext _holderWallet;
        private IAgentContext _holderWalletTwo;

        private readonly IEventAggregator _eventAggregator;
        private readonly IConnectionService _connectionService;

        private bool _routeMessage = true;
        private readonly ConcurrentBag<IAgentMessage> _messages = new ConcurrentBag<IAgentMessage>();

        public ConnectionTests()
        {
            _eventAggregator = new EventAggregator();

            var routingMock = new Mock<IMessageService>();
            routingMock.Setup(x =>
                    x.SendAsync(It.IsAny<Wallet>(), It.IsAny<IAgentMessage>(), It.IsAny<ConnectionRecord>(), It.IsAny<string>()))
                .Callback((Wallet _, IAgentMessage content, ConnectionRecord __, string ___) =>
                {
                    if (_routeMessage)
                        _messages.Add(content);
                    else
                        throw new AgentFrameworkException(ErrorCode.LedgerOperationRejected, "");
                })
                .Returns(Task.FromResult(false));

            _provisioningMock = new Mock<IProvisioningService>();
            _provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult(new ProvisioningRecord
                {
                    Endpoint = new AgentEndpoint {Uri = MockEndpointUri}
                }));

            _connectionService = new DefaultConnectionService(
                _eventAggregator,
                new DefaultWalletRecordService(),
                routingMock.Object,
                _provisioningMock.Object,
                new Mock<ILogger<DefaultConnectionService>>().Object);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(_issuerConfig, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            try
            {
                await Wallet.CreateWalletAsync(_holderConfig, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            try
            {
                await Wallet.CreateWalletAsync(_holderConfigTwo, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            _issuerWallet = new AgentContext {Wallet = await Wallet.OpenWalletAsync(_issuerConfig, Credentials)};
            _holderWallet = new AgentContext {Wallet = await Wallet.OpenWalletAsync(_holderConfig, Credentials)};
            _holderWalletTwo = new AgentContext {Wallet = await Wallet.OpenWalletAsync(_holderConfigTwo, Credentials)};
        }

        [Fact]
        public async Task ConnectUsingHttp()
        {
            var slim = new SemaphoreSlim(0, 1);

            var config1 = new WalletConfiguration() { Id = Guid.NewGuid().ToString() };
            var config2 = new WalletConfiguration() { Id = Guid.NewGuid().ToString() };
            var cred = new WalletCredentials() { Key = "2" };

            MockAgent agent1 = null;
            MockAgent agent2 = null;
            IAgentContext context1 = null;
            IAgentContext context2 = null;

            // Setup first agent runtime
            var result1 = await CreateDependency(config1, cred, new MockAgentHttpHandler(data => agent2.HandleAsync(data, context2.Wallet, context2.Pool)));
            var provider1 = result1.Item1;
            context1 = result1.Item2;
            agent1 = provider1.GetRequiredService<MockAgent>();
            var connectionService1 = provider1.GetRequiredService<IConnectionService>();

            // Setup second agent runtime
            var result2 = await CreateDependency(config2, cred, new MockAgentHttpHandler(data => agent1.HandleAsync(data, context1.Wallet, context1.Pool)));
            var provider2 = result2.Item1;
            context2 = result2.Item2;
            var connectionService2 = provider2.GetRequiredService<IConnectionService>();
            var messageService2 = provider2.GetRequiredService<IMessageService>();

            // Hook into response message event of second runtime to release semaphore
            var sub = provider2.GetRequiredService<IEventAggregator>().GetEventByType<ServiceMessageProcessingEvent>()
                .Where(x => x.MessageType == MessageTypes.ConnectionResponse)
                .Subscribe(x => slim.Release());
            agent2 = provider2.GetRequiredService<MockAgent>();

            // Invitation flow
            {
                var invitation = await connectionService1.CreateInvitationAsync(context1,
                    new InviteConfiguration { AutoAcceptConnection = true });

                var acceptInvitation =
                    await connectionService2.AcceptInvitationAsync(context2, invitation.Invitation);
                await messageService2.SendAsync(context2.Wallet, acceptInvitation.Request,
                    acceptInvitation.Connection, invitation.Invitation.RecipientKeys.First());

                // Wait for connection to be established or continue after 30 sec timeout
                await slim.WaitAsync(TimeSpan.FromSeconds(30));

                var connectionRecord1 = await connectionService1.GetAsync(context1, invitation.Connection.Id);
                var connectionRecord2 = await connectionService2.GetAsync(context2, acceptInvitation.Connection.Id);

                Assert.Equal(ConnectionState.Connected, connectionRecord1.State);
                Assert.Equal(ConnectionState.Connected, connectionRecord2.State);
                Assert.Equal(connectionRecord1.MyDid, connectionRecord2.TheirDid);
                Assert.Equal(connectionRecord1.TheirDid, connectionRecord2.MyDid);
            }

            // Cleanup
            {
                sub.Dispose();

                await context1.Wallet.CloseAsync();
                await context2.Wallet.CloseAsync();

                await Wallet.DeleteWalletAsync(config1.ToJson(), cred.ToJson());
                await Wallet.DeleteWalletAsync(config2.ToJson(), cred.ToJson());
            }
        }

        public async Task<(IServiceProvider, IAgentContext)> CreateDependency(
            WalletConfiguration configuration, 
            WalletCredentials credentials, 
            MockAgentHttpHandler handler)
        {
            var container = new ServiceCollection();
            container.AddAgentFramework();
            container.AddLogging();
            container.AddSingleton<MockAgent>();
            container.AddSingleton<HttpMessageHandler>(handler);
            container.AddSingleton(p => new HttpClient(p.GetRequiredService<HttpMessageHandler>()));
            var provider = container.BuildServiceProvider();

            await provider.GetService<IProvisioningService>().ProvisionAgentAsync(new ProvisioningConfiguration { WalletConfiguration = configuration, WalletCredentials = credentials, EndpointUri = new Uri("http://mock") });
            var context = new AgentContext { Wallet = await provider.GetService<IWalletService>().GetWalletAsync(configuration, credentials) };

            return (provider, context);
        }

        [Fact]
        public async Task CanCreateInvitationAsync()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() {ConnectionId = connectionId});

            var connection = await _connectionService.GetAsync(_issuerWallet, connectionId);

            Assert.False(connection.MultiPartyInvitation);
            Assert.Equal(ConnectionState.Invited, connection.State);
            Assert.Equal(connectionId, connection.Id);
        }

        [Fact]
        public async Task CanCreateMultiPartyInvitationAsync()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId, MultiPartyInvitation = true });

            var connection = await _connectionService.GetAsync(_issuerWallet, connectionId);

            Assert.True(connection.MultiPartyInvitation);
            Assert.Equal(ConnectionState.Invited, connection.State);
            Assert.Equal(connectionId, connection.Id);
        }

        [Fact]
        public async Task AcceptRequestThrowsExceptionConnectionNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.AcceptRequestAsync(_issuerWallet, "bad-connection-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task AcceptRequestThrowsExceptionConnectionInvalidState()
        {
            var connectionId = Guid.NewGuid().ToString();
            
            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration { ConnectionId = connectionId, AutoAcceptConnection = false });

            //Process a connection request
            var connectionRecord = await _connectionService.GetAsync(_issuerWallet, connectionId);
            _issuerWallet.Connection = connectionRecord;

            await _connectionService.ProcessRequestAsync(_issuerWallet, new ConnectionRequestMessage
            {
                Connection = new Connection {
                    Did = "did:sov:EYS94e95kf6LXF49eARL76",
                    DidDoc = new ConnectionRecord
                    {
                        MyVk = "~LGkX716up2KAimNfz11HRr"
                    }.MyDidDoc(await _provisioningMock.Object.GetProvisioningAsync(_issuerWallet.Wallet))
                }
            });

            //Accept the connection request
            await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId);

            //Now try and accept it again
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task RevokeInvitationThrowsConnectionNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.RevokeInvitationAsync(_issuerWallet, "bad-connection-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task RevokeInvitationThrowsConnectionInvalidState()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration { ConnectionId = connectionId, AutoAcceptConnection = false });

            //Process a connection request
            var connectionRecord = await _connectionService.GetAsync(_issuerWallet, connectionId);
            _issuerWallet.Connection = connectionRecord;
            await _connectionService.ProcessRequestAsync(_issuerWallet, new ConnectionRequestMessage
            {
                Connection = new Connection { 
                    Did = "did:sov:EYS94e95kf6LXF49eARL76",
                    DidDoc = new ConnectionRecord
                    {
                        MyVk = "~LGkX716up2KAimNfz11HRr"
                    }.MyDidDoc(await _provisioningMock.Object.GetProvisioningAsync(_issuerWallet.Wallet))
                }
            });

            //Accept the connection request
            await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId);

            //Now try and revoke invitation
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.RevokeInvitationAsync(_issuerWallet, connectionId));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task CanRevokeInvitation()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId });

            var connection = await _connectionService.GetAsync(_issuerWallet, connectionId);

            Assert.False(connection.MultiPartyInvitation);
            Assert.Equal(ConnectionState.Invited, connection.State);
            Assert.Equal(connectionId, connection.Id);

            await _connectionService.RevokeInvitationAsync(_issuerWallet, connectionId);

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task CanEstablishConnectionAsync()
        {
            int events = 0;
            _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
                .Where(_ => (_.MessageType == MessageTypes.ConnectionRequest ||
                             _.MessageType == MessageTypes.ConnectionResponse))
                .Subscribe(_ =>
                {
                    events++;
                });


            var (connectionIssuer, connectionHolder) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            Assert.True(events == 2);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolder.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolder.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolder.MyDid);

            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
        }

        [Fact]
        public async Task CanEstablishConnectionsWithMultiPartyInvitationAsync()
        {
            var invite = await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration { MultiPartyInvitation = true });

            var (connectionIssuer, connectionHolderOne) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet, invite, invite.Connection.Id);

            var (connectionIssuerTwo, connectionHolderTwo) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWalletTwo, invite, invite.Connection.Id);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolderOne.State);

            Assert.Equal(ConnectionState.Connected, connectionIssuerTwo.State);
            Assert.Equal(ConnectionState.Connected, connectionHolderTwo.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolderOne.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolderOne.MyDid);

            Assert.Equal(connectionIssuerTwo.MyDid, connectionHolderTwo.TheirDid);
            Assert.Equal(connectionIssuerTwo.TheirDid, connectionHolderTwo.MyDid);

            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
            Assert.Equal(connectionIssuerTwo.Endpoint.Uri, MockEndpointUri);
        }

        [Fact]
        public async Task Test()
        {
            string connectionRequestJson =
                "{\n  \"@type\": \"did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/request\",\n  \"label\": \"test\",\n  \"connection\": {\n    \"DID\": \"DKF8gGMQoUrj1rDVPVfFBW\",\n    \"DIDDoc\": {\n      \"@context\": \"https://w3id.org/did/v1\",\n      \"id\": \"DKF8gGMQoUrj1rDVPVfFBW\",\n      \"publicKey\": [\n        {\n          \"id\": \"DKF8gGMQoUrj1rDVPVfFBW#keys-1\",\n          \"type\": \"Ed25519VerificationKey2018\",\n          \"controller\": \"DKF8gGMQoUrj1rDVPVfFBW\",\n          \"publicKeyBase58\": \"7iHeiEP6QU56oe6nGcSsVhvYPLJoNKaX6Dc727GM1UxB\"\n        }\n      ],\n      \"service\": [\n        {\n          \"id\": \"DKF8gGMQoUrj1rDVPVfFBW;indy\",\n          \"type\": \"IndyAgent\",\n          \"recipientKeys\": [\n            \"7iHeiEP6QU56oe6nGcSsVhvYPLJoNKaX6Dc727GM1UxB\"\n          ],\n          \"serviceEndpoint\": \"http://localhost:9000/indy\"\n        }\n      ]\n    }\n  },\n  \"@id\": \"3ec57100-6e44-4923-94ca-6f6aabe01439\"\n}";

            var connectionRequest = JsonConvert.DeserializeObject<ConnectionRequestMessage>(connectionRequestJson);

            await _connectionService.ProcessRequestAsync(_issuerWallet, connectionRequest);
        }

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.Wallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.Wallet.CloseAsync();
            if (_holderWalletTwo != null) await _holderWalletTwo.Wallet.CloseAsync();

            await Wallet.DeleteWalletAsync(_issuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(_holderConfig, Credentials);
            await Wallet.DeleteWalletAsync(_holderConfigTwo, Credentials);
        }
    }
}