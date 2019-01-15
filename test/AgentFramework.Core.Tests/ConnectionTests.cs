using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class ConnectionTests : IAsyncLifetime
    {
        private readonly string _issuerConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}"; 
        private readonly string _holderConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";
        
        private readonly Mock<IProvisioningService> _provisioningMock;

        private Wallet _issuerWallet;
        private Wallet _holderWallet;

        private readonly IConnectionService _connectionService;

        private bool _routeMessage = true;
        private readonly ConcurrentBag<IAgentMessage> _messages = new ConcurrentBag<IAgentMessage>();

        public ConnectionTests()
        {
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

            _issuerWallet = await Wallet.OpenWalletAsync(_issuerConfig, Credentials);
            _holderWallet = await Wallet.OpenWalletAsync(_holderConfig, Credentials);
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
                Endpoint = new AgentEndpoint
                {
                    Did = "EYS94e95kf6LXF49eARL76",
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
                Endpoint = new AgentEndpoint
                {
                    Did = "EYS94e95kf6LXF49eARL76",
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
        public async Task CanEstablishConnectionAsync()
        {
            var (connectionIssuer, connectionHolder) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolder.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolder.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolder.MyDid);

            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
        }
         
        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(_issuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(_holderConfig, Credentials);
        }
    }
}