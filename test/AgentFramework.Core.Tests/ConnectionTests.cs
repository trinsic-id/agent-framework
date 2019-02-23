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
    public class AgentHttpHandler : HttpMessageHandler
    {
        public IServiceProvider AgentBase { get; }
        public Wallet Wallet { get; }
        public Pool Pool { get; }

        public AgentHttpHandler(IServiceProvider agentBase, IAgentContext context)
        {
            AgentBase = agentBase;
            Wallet = context.Wallet;
            Pool = context.Pool;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ab = AgentBase.GetService<MockAgent>();

            if (request.Method != HttpMethod.Post)
            {
                throw new Exception("Invalid http method");
            }

            var body = await request.Content.ReadAsByteArrayAsync();
            await ab.HandleAsync(body, Wallet, Pool);

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
            try
            {
                var config1 = new WalletConfiguration() {Id = Guid.NewGuid().ToString()};
                var config2 = new WalletConfiguration() { Id = Guid.NewGuid().ToString() };
                var cred = new WalletCredentials() {Key = "2"};

                var firstContainer = new ServiceCollection();
                firstContainer.AddAgentFramework();
                firstContainer.AddLogging();
                firstContainer.AddSingleton<IAgentContext>(_issuerWallet);
                firstContainer.AddSingleton<MockAgent>();
                firstContainer.AddSingleton(provider => new HttpClient(
                    new AgentHttpHandler(provider,
                        provider.GetService<IAgentContext>())));
                var firstProvider = firstContainer.BuildServiceProvider();
                var firstConnection = firstProvider.GetRequiredService<IConnectionService>();
                var firstMessageService = firstProvider.GetRequiredService<IMessageService>();

                var secondContainer = new ServiceCollection();
                secondContainer.AddAgentFramework();
                secondContainer.AddLogging();
                secondContainer.AddSingleton(_issuerWallet);
                secondContainer.AddSingleton<AgentHttpHandler>();
                secondContainer.AddSingleton<MockAgent>();
                secondContainer.AddSingleton(provider => new HttpClient(provider.GetService<AgentHttpHandler>()));
                var secondProvider = secondContainer.BuildServiceProvider();
                var secondConnection = secondProvider.GetRequiredService<IConnectionService>();
                var secondMessageService = firstProvider.GetRequiredService<IMessageService>();

                await Task.Yield();

                await firstProvider.GetService<IProvisioningService>().ProvisionAgentAsync(new ProvisioningConfiguration(){ WalletConfiguration = config1, WalletCredentials = cred, EndpointUri = new Uri("http://mock")});
                var firstWallet = await firstProvider.GetService<IWalletService>().GetWalletAsync(config1, cred);

                await secondProvider.GetService<IProvisioningService>().ProvisionAgentAsync(new ProvisioningConfiguration() { WalletConfiguration = config2, WalletCredentials = cred, EndpointUri = new Uri("http://mock") });
                var secondWallet = await secondProvider.GetService<IWalletService>().GetWalletAsync(config2, cred);


                var invitation = await firstConnection.CreateInvitationAsync(new AgentContext(){Wallet = firstWallet},
                    new InviteConfiguration {AutoAcceptConnection = true});
                await firstMessageService.SendAsync(firstWallet, invitation.Invitation, invitation.Connection);

                var acceptInvitation =
                    await secondConnection.AcceptInvitationAsync(new AgentContext(){ Wallet = secondWallet}, invitation.Invitation);
                await secondMessageService.SendAsync(secondWallet, acceptInvitation.Request,
                    acceptInvitation.Connection, invitation.Invitation.RecipientKeys.First());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
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