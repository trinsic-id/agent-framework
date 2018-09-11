using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class CredentialTests : IAsyncLifetime
    {
        private const string IssuerConfig = "{\"id\":\"issuer_test_wallet\"}";
        private const string HolderConfig = "{\"id\":\"holder_test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";

        private Wallet _issuerWallet;
        private Wallet _holderWallet;

        private readonly IConnectionService _connectionService;

        private readonly ConcurrentBag<IEnvelopeMessage> _messages = new ConcurrentBag<IEnvelopeMessage>();

        public CredentialTests()
        {
            var messageSerializer = new MessageSerializer();

            var routingMock = new Mock<IRouterService>();
            routingMock.Setup(x => x.ForwardAsync(It.IsNotNull<IEnvelopeMessage>(), It.IsAny<AgentEndpoint>()))
                .Callback((IEnvelopeMessage content, AgentEndpoint endpoint) => { _messages.Add(content); })
                .Returns(Task.CompletedTask);

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult(new ProvisioningRecord { Endpoint = new AgentEndpoint { Uri = MockEndpointUri } }));

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
            finally
            {
                _issuerWallet = await Wallet.OpenWalletAsync(IssuerConfig, Credentials);
                _holderWallet = await Wallet.OpenWalletAsync(HolderConfig, Credentials);
            }
        }

        public async Task CredentialIssuanceDemo()
        {
            var (issuerConnection, holderConnection) = await EstablishConnectionAsync();
        }

        private async Task<(ConnectionRecord issuer, ConnectionRecord holder)> EstablishConnectionAsync()
        {
            // Create invitation by the issuer
            var issuerConnectionId = Guid.NewGuid().ToString();
            var invitation = await _connectionService.CreateInvitationAsync(_issuerWallet, issuerConnectionId);
            var connectionIssuer = await _connectionService.GetAsync(_issuerWallet, issuerConnectionId);

            // Holder accepts invitation and sends a message request
            var holderConnectionId = await _connectionService.AcceptInvitationAsync(_holderWallet, invitation);
            var connectionHolder = await _connectionService.GetAsync(_holderWallet, holderConnectionId);

            // Issuer processes incoming message
            var issuerMessage = _messages.OfType<ForwardToKeyEnvelopeMessage>()
                .First(x => x.Key == connectionIssuer.Tags.Single(item => item.Key == "connectionKey").Value);

            var requestMessage = GetContentMessage(issuerMessage) as ConnectionRequest;
            Assert.NotNull(requestMessage);

            // Issuer stores and accepts the request
            await _connectionService.StoreRequestAsync(_issuerWallet, requestMessage);
            await _connectionService.AcceptRequestAsync(_issuerWallet, issuerConnectionId);

            // Holder processes incoming message
            var holderMessage = _messages.OfType<ForwardEnvelopeMessage>()
                .First(x => x.To == connectionHolder.MyDid);

            var responseMessage = GetContentMessage(holderMessage) as ConnectionResponse;
            Assert.NotNull(responseMessage);

            // Holder accepts response message
            await _connectionService.AcceptResponseAsync(_holderWallet, responseMessage);

            // Retrieve updated connection state for both issuer and holder
            connectionIssuer = await _connectionService.GetAsync(_issuerWallet, issuerConnectionId);
            connectionHolder = await _connectionService.GetAsync(_holderWallet, holderConnectionId);

            return (connectionIssuer, connectionHolder);
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
