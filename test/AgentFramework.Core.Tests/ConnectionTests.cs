using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Helpers;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Runtime;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class ConnectionTests : IAsyncLifetime
    {
        private readonly WalletConfiguration _issuerWalletConfig = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        private readonly WalletConfiguration _holderWalletConfig = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        private readonly WalletConfiguration _agencyWalletConfig = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
        private readonly WalletCredentials _walletCredentials = new WalletCredentials {Key = "test_wallet_key"};

        private const string MockEndpointUri = "http://mock";

        private readonly IConnectionService _connectionService;
        private readonly IProvisioningService _provisioningService;
        private readonly IRouterService _routerService;

        private Wallet _issuerWallet;
        private Wallet _holderWallet;
        private Wallet _agencyWallet;

        private bool _routeMessage = true;
        private readonly ConcurrentBag<IAgentMessage> _messages = new ConcurrentBag<IAgentMessage>();

        public ConnectionTests()
        {
            var walletRecordService = new DefaultWalletRecordService(new DateTimeHelper());

            var messageSerializer = new DefaultMessageSerializer();

            var messagingMock = new Mock<IMessagingService>();
            messagingMock.Setup(x =>
                    x.SendAsync(It.IsAny<Wallet>(), It.IsAny<IAgentMessage>(), It.IsAny<ConnectionRecord>(), It.IsAny<string>()))
                .Callback((Wallet _, IAgentMessage content, ConnectionRecord __, string ___) =>
                {
                    if (_routeMessage)
                        _messages.Add(content);
                    else
                        throw new AgentFrameworkException(ErrorCode.LedgerOperationRejected, "");
                })
                .Returns(Task.FromResult(false));

            _provisioningService = new DefaultProvisioningService(walletRecordService, new DefaultWalletService());

            _routerService = new DefaultRouterService(walletRecordService, messagingMock.Object, new Mock<ILogger<DefaultRouterService>>().Object);

            _connectionService = new DefaultConnectionService(
                walletRecordService,
                messagingMock.Object,
                _provisioningService,
                messageSerializer,
                new Mock<ILogger<DefaultConnectionService>>().Object);
        }

        public async Task InitializeAsync()
        {
            await _provisioningService.ProvisionAgentAsync(
                new ProvisioningConfiguration(_issuerWalletConfig,
                    _walletCredentials,
                    new IDidService[] {
                        new AgentService
                        {
                            ServiceEndpoint = MockEndpointUri
                        },
                        new AgencyService
                        {
                            ServiceEndpoint = MockEndpointUri
                        }
                    }));

            await _provisioningService.ProvisionAgentAsync(
                new ProvisioningConfiguration(_holderWalletConfig,
                    _walletCredentials, 
                    new IDidService[] {
                        new AgentService
                        {
                            ServiceEndpoint = MockEndpointUri
                        },
                        new AgencyService
                        {
                            ServiceEndpoint = MockEndpointUri
                        }
                        }));

            await _provisioningService.ProvisionAgentAsync(
                new ProvisioningConfiguration(_agencyWalletConfig,
                    _walletCredentials,
                    new AgentService
                    {
                        ServiceEndpoint = MockEndpointUri
                    }
                    ));

            _issuerWallet = await Wallet.OpenWalletAsync(_issuerWalletConfig.ToJson(), _walletCredentials.ToJson());
            _holderWallet = await Wallet.OpenWalletAsync(_holderWalletConfig.ToJson(), _walletCredentials.ToJson());
            _agencyWallet = await Wallet.OpenWalletAsync(_agencyWalletConfig.ToJson(), _walletCredentials.ToJson());
        }

        [Fact]
        public async Task CanCreateInvitationAsync()
        {
            var connectionId = Guid.NewGuid().ToString();

            var invitation = await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() {ConnectionId = connectionId});

            var connection = await _connectionService.GetAsync(_issuerWallet, connectionId);

            Assert.Equal(ConnectionState.Invited, connection.State);
            Assert.Equal(connectionId, connection.Id);
        }
        
        [Fact]
        public async Task AcceptInviteThrowsExceptionUnableToSendA2AMessage()
        {
            var connectionId = Guid.NewGuid().ToString();

            var invitation = await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId });

            _routeMessage = false;
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.AcceptInvitationAsync(_holderWallet, invitation));
            _routeMessage = true;

            Assert.True(ex.ErrorCode == ErrorCode.A2AMessageTransmissionError);
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

            await _connectionService.ProcessRequestAsync(_issuerWallet, new ConnectionRequestMessage
            {
                Did = "EYS94e95kf6LXF49eARL76",
                Verkey = "~LGkX716up2KAimNfz11HRr",
                Endpoint = new AgencyService
                {
                    Verkey = "~LGkX716up2KAimNfz11HRr"
                },
                Type = MessageTypes.ConnectionRequest
            }, connectionRecord);

            //Accept the connection request
            await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId);

            //Now try and accept it again
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task AcceptRequestThrowsExceptionUnableToSendA2AMessage()
        {
            
            var connectionId = Guid.NewGuid().ToString();
            
            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId, AutoAcceptConnection = false });

            //Process a connection request
            var connectionRecord = await _connectionService.GetAsync(_issuerWallet, connectionId);

            await _connectionService.ProcessRequestAsync(_issuerWallet, new ConnectionRequestMessage
            {
                Did = "EYS94e95kf6LXF49eARL76",
                Verkey = "~LGkX716up2KAimNfz11HRr",
                Endpoint = new AgencyService
                {
                    Verkey = "~LGkX716up2KAimNfz11HRr"
                },
                Type = MessageTypes.ConnectionRequest
            }, connectionRecord);
            
            //Now try and accept it again
            _routeMessage = false;
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _connectionService.AcceptRequestAsync(_issuerWallet, connectionId));
            _routeMessage = true;

            Assert.True(ex.ErrorCode == ErrorCode.A2AMessageTransmissionError);
            //Process a connection request
            var connectionRecordRefetched = await _connectionService.GetAsync(_issuerWallet, connectionId);

            Assert.True(connectionRecordRefetched.State == ConnectionState.Negotiating);
        }

        [Fact]
        public async Task CanEstablishDirectAutomaticConnectionAsync()
        {
            var (connectionIssuer, connectionHolder) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet, true);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolder.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolder.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolder.MyDid);

            Assert.True(connectionIssuer.Services.Count() == 1);
            var agency = connectionIssuer.Services[0] as AgentService;
            Assert.True(agency?.ServiceEndpoint == MockEndpointUri);

            Assert.True(connectionHolder.Services.Count() == 1);
            agency = connectionHolder.Services[0] as AgentService;
            Assert.True(agency?.ServiceEndpoint == MockEndpointUri);
        }

        [Fact]
        public async Task CanEstablishDirectConnectionAsync()
        {
            var (connectionIssuer, connectionHolder) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolder.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolder.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolder.MyDid);

            Assert.True(connectionIssuer.Services.Count() == 1);
            var agency = connectionIssuer.Services[0] as AgentService;
            Assert.True(agency?.ServiceEndpoint == MockEndpointUri);

            Assert.True(connectionHolder.Services.Count() == 1);
            agency = connectionHolder.Services[0] as AgentService;
            Assert.True(agency?.ServiceEndpoint == MockEndpointUri);
        }

        [Fact]
        public async Task CanEstablishConnectionWithAgencyAsync()
        {
            var (connectionIssuer, connectionHolder) = await Scenarios.EstablishConnectionUsingAgencyAsync(
                _connectionService, _routerService, _messages, _issuerWallet, _holderWallet, _agencyWallet);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolder.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolder.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolder.MyDid);

            Assert.True(connectionIssuer.Services.Count() == 1);
            var agency = connectionIssuer.Services[0] as AgencyService;
            Assert.True(agency?.ServiceEndpoint == MockEndpointUri);

            Assert.True(connectionHolder.Services.Count() == 1);
            agency = connectionHolder.Services[0] as AgencyService;
            Assert.True(agency?.ServiceEndpoint == MockEndpointUri);
        }

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(_issuerWalletConfig.ToJson(), _walletCredentials.ToJson());
            await Wallet.DeleteWalletAsync(_holderWalletConfig.ToJson(), _walletCredentials.ToJson());
        }
    }
}