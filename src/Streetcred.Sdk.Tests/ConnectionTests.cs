using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Runtime;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class ConnectionTests : IAsyncLifetime
    {
        private const string IssuerConfig = "{\"id\":\"issuer_test_wallet\"}";
        private const string HolderConfig = "{\"id\":\"holder_test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";

        private Wallet _issuerWallet;
        private Wallet _holderWallet;

        private readonly IConnectionService _connectionService;

        private readonly ConcurrentBag<IEnvelopeMessage> _messages = new ConcurrentBag<IEnvelopeMessage>();

        public ConnectionTests()
        {
            var messageSerializer = new MessageSerializer();

            var routingMock = new Mock<IRouterService>();
            routingMock.Setup(x => x.ForwardAsync(It.IsNotNull<IEnvelopeMessage>(), It.IsAny<AgentEndpoint>()))
                .Callback((IEnvelopeMessage content, AgentEndpoint endpoint) => { _messages.Add(content); })
                .Returns(Task.CompletedTask);

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult(new ProvisioningRecord
                {
                    Endpoint = new AgentEndpoint {Uri = MockEndpointUri}
                }));

            _connectionService = new ConnectionService(
                new WalletRecordService(),
                routingMock.Object,
                provisioningMock.Object,
                messageSerializer,
                new Mock<ILogger<ConnectionService>>().Object);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(IssuerConfig, Credentials);
                await Wallet.CreateWalletAsync(HolderConfig, Credentials);
            }
            catch (WalletExistsException)
            {
            }
            finally
            {
                _issuerWallet = await Wallet.OpenWalletAsync(IssuerConfig, Credentials);
                _holderWallet = await Wallet.OpenWalletAsync(HolderConfig, Credentials);
            }
        }

        [Fact]
        public async Task CanCreateInvitationAsync()
        {
            var connectionId = Guid.NewGuid().ToString();

            var invitation = await _connectionService.CreateInvitationAsync(_issuerWallet,
                new CreateInviteConfiguration() {ConnectionId = connectionId});

            var connection = await _connectionService.GetAsync(_issuerWallet, connectionId);

            Assert.Equal(ConnectionState.Invited, connection.State);
            Assert.Equal(connectionId, connection.GetId());
        }

        [Fact]
        public async Task CanEstablishConnectionAsync()
        {
            // Create invitation by the issuer
            var issuerConnectionId = Guid.NewGuid().ToString();

            var inviteConfig = new CreateInviteConfiguration()
            {
                ConnectionId = issuerConnectionId,
                MyAlias = new ConnectionAlias()
                {
                    Name = "Issuer",
                    ImageUrl = "www.issuerdomain.com/profilephoto"
                },
                TheirAlias = new ConnectionAlias()
                {
                    Name = "Holder",
                    ImageUrl = "www.holderdomain.com/profilephoto"
                }
            };

            // Issuer creates an invitation
            var invitation = await _connectionService.CreateInvitationAsync(_issuerWallet, inviteConfig);

            var connectionIssuer = await _connectionService.GetAsync(_issuerWallet, issuerConnectionId);

            Assert.Equal(ConnectionState.Invited, connectionIssuer.State);
            Assert.True(invitation.Name == inviteConfig.MyAlias.Name &&
                        invitation.ImageUrl == inviteConfig.MyAlias.ImageUrl);

            // Holder accepts invitation and sends a message request
            var holderConnectionId = await _connectionService.AcceptInvitationAsync(_holderWallet, invitation);
            var connectionHolder = await _connectionService.GetAsync(_holderWallet, holderConnectionId);

            Assert.Equal(ConnectionState.Negotiating, connectionHolder.State);

            // Issuer processes incoming message
            var issuerMessage = _messages.OfType<ForwardToKeyEnvelopeMessage>()
                .First(x => x.Type.Contains(connectionIssuer.Tags.Single(item => item.Key == "connectionKey").Value));

            var requestMessage = GetContentMessage(issuerMessage) as ConnectionRequest;
            Assert.NotNull(requestMessage);

            // Issuer stores the connection request
            await _connectionService.StoreRequestAsync(_issuerWallet, requestMessage);

            connectionIssuer = await _connectionService.GetAsync(_issuerWallet, issuerConnectionId);
            Assert.Equal(ConnectionState.Negotiating, connectionIssuer.State);

            // Issuer accepts the connection request
            await _connectionService.AcceptRequestAsync(_issuerWallet, issuerConnectionId);

            connectionIssuer = await _connectionService.GetAsync(_issuerWallet, issuerConnectionId);
            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);

            // Holder processes incoming message
            var holderMessage = _messages.OfType<ForwardEnvelopeMessage>()
                .First(x => x.Type.Contains(connectionHolder.MyDid));

            var responseMessage = GetContentMessage(holderMessage) as ConnectionResponse;
            Assert.NotNull(responseMessage);

            // Holder accepts response message
            await _connectionService.AcceptResponseAsync(_holderWallet, responseMessage);

            // Retrieve updated connection state for both issuer and holder
            connectionIssuer = await _connectionService.GetAsync(_issuerWallet, issuerConnectionId);
            connectionHolder = await _connectionService.GetAsync(_holderWallet, holderConnectionId);

            Assert.True(connectionIssuer.Alias.Name == inviteConfig.TheirAlias.Name &&
                        connectionIssuer.Alias.ImageUrl == inviteConfig.TheirAlias.ImageUrl);
            Assert.True(connectionHolder.Alias.Name == inviteConfig.MyAlias.Name &&
                        connectionHolder.Alias.ImageUrl == inviteConfig.MyAlias.ImageUrl);

            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);
            Assert.Equal(ConnectionState.Connected, connectionHolder.State);

            Assert.Equal(connectionIssuer.MyDid, connectionHolder.TheirDid);
            Assert.Equal(connectionIssuer.TheirDid, connectionHolder.MyDid);

            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
            Assert.Equal(connectionIssuer.Endpoint.Uri, MockEndpointUri);
        }

        private IContentMessage GetContentMessage(IEnvelopeMessage message)
            => JsonConvert.DeserializeObject<IContentMessage>(message.Content);

        public async Task DisposeAsync()
        {
            await _issuerWallet.CloseAsync();
            await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(IssuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(HolderConfig, Credentials);
        }
    }
}