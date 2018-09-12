using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
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
using Streetcred.Sdk.Utils;
using Hyperledger.Indy.PoolApi;
using System.IO;
using Streetcred.Sdk.Model.Credentials;
using Hyperledger.Indy.AnonCredsApi;

namespace Streetcred.Sdk.Tests
{
    public class CredentialTests : IAsyncLifetime
    {
        private const string PoolName = "CredentialTestPool";
        private const string IssuerConfig = "{\"id\":\"issuer_credential_test_wallet\"}";
        private const string HolderConfig = "{\"id\":\"holder_credential_test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";
        private const string MasterSecretId = "DefaultMasterSecret";

        private Pool _pool;
        private Wallet _issuerWallet;
        private Wallet _holderWallet;

        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;

        private SchemaService _schemaService;
        private PoolService _poolService;

        private readonly ConcurrentBag<IEnvelopeMessage> _messages = new ConcurrentBag<IEnvelopeMessage>();

        public CredentialTests()
        {
            var messageSerializer = new MessageSerializer();
            var recordService = new WalletRecordService();
            var ledgerService = new LedgerService();
            var tailsService = new TailsService();
            _poolService = new PoolService();
            _schemaService = new SchemaService(recordService, ledgerService, tailsService);

            var routingMock = new Mock<IRouterService>();
            routingMock.Setup(x => x.ForwardAsync(It.IsNotNull<IEnvelopeMessage>(), It.IsAny<AgentEndpoint>()))
                .Callback((IEnvelopeMessage content, AgentEndpoint endpoint) => { _messages.Add(content); })
                .Returns(Task.CompletedTask);

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                            .Returns(Task.FromResult(new ProvisioningRecord { Endpoint = new AgentEndpoint { Uri = MockEndpointUri }, MasterSecretId = MasterSecretId }));

            _connectionService = new ConnectionService(
                recordService,
                routingMock.Object,
                provisioningMock.Object,
                messageSerializer,
                new Mock<ILogger<ConnectionService>>().Object);

            _credentialService = new CredentialService(
                routingMock.Object,
                ledgerService,
                _connectionService,
                recordService,
                messageSerializer,
                _schemaService,
                tailsService,
                provisioningMock.Object,
                new Mock<ILogger<CredentialService>>().Object);
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

            try 
            {
                await _poolService.CreatePoolAsync(PoolName, Path.GetFullPath("pool_genesis.txn"));
            }
            finally
            {
                _pool = await _poolService.GetPoolAsync(PoolName);
            }
        }

        [Fact]
        public async Task CredentialIssuanceDemo()
        {
            if (_pool == null) throw new Exception("This test reuquires a pool connection.");

            var (issuerConnection, holderConnection) = await EstablishConnectionAsync();

            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet, new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            var schemaId = await _schemaService.CreateSchemaAsync(_pool, _issuerWallet, issuer.Did, $"Test-Schema-{Guid.NewGuid().ToString()}", "1.0", new[] { "first_name", "last_name" });
            var definitionId = await _schemaService.CreateCredentialDefinitionAsync(_pool, _issuerWallet, schemaId, issuer.Did, true, 100);

            await _credentialService.SendOfferAsync(definitionId, issuerConnection.GetId(), _issuerWallet, issuer.Did);

            var credentialOffer = FindContentMessage<CredentialOffer>();

            var holderCredentialId = await _credentialService.StoreOfferAsync(_holderWallet, credentialOffer, holderConnection.GetId());

            await AnonCreds.ProverCreateMasterSecretAsync(_holderWallet, MasterSecretId);

            await _credentialService.AcceptOfferAsync(_holderWallet, _pool, holderCredentialId, new Dictionary<string, string>
            {
                { "first_name", "Jane" },
                { "last_name", "Doe" }
            });

            var credentialRequest = FindContentMessage<CredentialRequest>();
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

        private T FindContentMessage<T>() where T: IContentMessage
            => _messages.Select(GetContentMessage).OfType<T>().Single();

        public async Task DisposeAsync()
        {
            await _issuerWallet.CloseAsync();
            await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(IssuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(HolderConfig, Credentials);

            await _pool.CloseAsync();
            await Pool.DeletePoolLedgerConfigAsync(PoolName);
        }
    }
}
