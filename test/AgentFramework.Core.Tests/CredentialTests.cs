using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class CredentialTests : IAsyncLifetime
    {
        private readonly string _poolName = $"Pool{Guid.NewGuid()}";
        private readonly string _issuerConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private readonly string _holderConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";
        private const string MasterSecretId = "DefaultMasterSecret";
        
        private Wallet _issuerWallet;
        private Wallet _holderWallet;

        private readonly Mock<IProvisioningService> _provisioningMock;
        private readonly Mock<IRouterService> _badRoutingMock;

        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;

        private readonly ISchemaService _schemaService;
        private readonly IPoolService _poolService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly IWalletRecordService _recordService;
        private readonly ILedgerService _ledgerService;
        private readonly ITailsService _tailsService;

        private readonly ConcurrentBag<IAgentMessage> _messages = new ConcurrentBag<IAgentMessage>();

        public CredentialTests()
        {
            _messageSerializer = new DefaultMessageSerializer();
            _recordService = new DefaultWalletRecordService();
            _ledgerService = new DefaultLedgerService();

            _poolService = new DefaultPoolService();

            _badRoutingMock = new Mock<IRouterService>();
            _badRoutingMock.Setup(x => x.SendAsync(It.IsAny<Wallet>(), It.IsAny<IAgentMessage>(), It.IsAny<ConnectionRecord>()))
                .Callback((Wallet _, IAgentMessage content, ConnectionRecord __) => { })
                .Returns(Task.FromResult(false));

            var routingMock = new Mock<IRouterService>();
            routingMock.Setup(x => x.SendAsync(It.IsAny<Wallet>(), It.IsAny<IAgentMessage>(), It.IsAny<ConnectionRecord>()))
                .Callback((Wallet _, IAgentMessage content, ConnectionRecord __) => { _messages.Add(content); })
                .Returns(Task.FromResult(true));

            _provisioningMock = new Mock<IProvisioningService>();
            _provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult(new ProvisioningRecord
                {
                    Endpoint = new AgentEndpoint {Uri = MockEndpointUri},
                    MasterSecretId = MasterSecretId,
                    TailsBaseUri = MockEndpointUri
                }));

            _tailsService = new DefaultTailsService(_ledgerService);
            _schemaService = new DefaultSchemaService(_recordService, _ledgerService, _tailsService);

            _connectionService = new DefaultConnectionService(
                _recordService,
                routingMock.Object,
                _provisioningMock.Object,
                _messageSerializer,
                new Mock<ILogger<DefaultConnectionService>>().Object);

            _credentialService = new DefaultCredentialService(
                routingMock.Object,
                _ledgerService,
                _connectionService,
                _recordService,
                _messageSerializer,
                _schemaService,
                _tailsService,
                _provisioningMock.Object,
                new Mock<ILogger<DefaultCredentialService>>().Object);
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

        private async Task<Pool> InitializePoolAsync()
        {
            try
            {
                await _poolService.CreatePoolAsync(_poolName, Path.GetFullPath("pool_genesis.txn"));
            }
            catch (PoolLedgerConfigExistsException)
            {
                // OK
            }
            return await _poolService.GetPoolAsync(_poolName, 2);
        }

        /// <summary>
        /// This test requires a local running node accessible at 127.0.0.1
        /// </summary>
        /// <returns>The issuance demo.</returns>
        [Fact]
        public async Task CredentialIssuanceDemo()
        {
            var pool = await InitializePoolAsync();

            // Setup secure connection between issuer and holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            var (issuerCredential, holderCredential) = await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, pool, MasterSecretId, false);

            Assert.Equal(issuerCredential.State, holderCredential.State);
            Assert.Equal(CredentialState.Issued, issuerCredential.State);

            await DisposePoolAsync(pool);
        }

        [Fact]
        public async Task CreateOfferAsyncThrowsExceptionConnectionNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.CreateOfferAsync(_issuerWallet, new OfferConfiguration { ConnectionId = "bad-connection-id" }));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task CreateOfferAsyncThrowsExceptionConnectionInvalidState()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId, AutoAcceptConnection = false });

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.CreateOfferAsync(_issuerWallet, new OfferConfiguration { ConnectionId = connectionId }));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task SendOfferAsyncThrowsExceptionConnectionNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.SendOfferAsync(_issuerWallet, new OfferConfiguration {ConnectionId = "bad-connection-id"}));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task SendOfferAsyncThrowsExceptionConnectionInvalidState()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId, AutoAcceptConnection = false });

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.SendOfferAsync(_issuerWallet, new OfferConfiguration
            {
                ConnectionId = connectionId
            }));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task SendOfferAsyncThrowsExceptionUnableToSendA2AMessage()
        {
            var pool = await InitializePoolAsync();

            var credentialService = new DefaultCredentialService(
                _badRoutingMock.Object,
                new DefaultLedgerService(), 
                _connectionService,
                _recordService,
                _messageSerializer,
                _schemaService,
                _tailsService,
                _provisioningMock.Object,
                new Mock<ILogger<DefaultCredentialService>>().Object);

            var (issuerConnection, _) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);
            
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            (var credId, var schemaId) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(pool, _issuerWallet, _schemaService, issuer.Did,
                new[] { "dummy_attr" });

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await credentialService.SendOfferAsync(_issuerWallet, new OfferConfiguration
            {
                ConnectionId = issuerConnection.ConnectionId,
                CredentialDefinitionId = credId
            }));

            Assert.True(ex.ErrorCode == ErrorCode.A2AMessageTransmissionFailure);

            await DisposePoolAsync(pool);
        }

        [Fact]
        public async Task ProcessCredentialRequestThrowsCredentialNotFound()
        {
            var pool = await InitializePoolAsync();

            var (issuerConnection, _) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);
            
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.ProcessCredentialRequestAsync(_issuerWallet,
                new CredentialRequestMessage
                {
                    OfferJson = "{ \"nonce\":\"bad-nonce\" }"
                }, issuerConnection));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);

            await DisposePoolAsync(pool);
        }
        
        [Fact]
        public async Task RejectCredentialRequestThrowsExceptionCredentialNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async() => await _credentialService.RejectCredentialRequestAsync(_holderWallet, "bad-credential-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        //TODO implement test
        //[Fact]
        //public async Task RejectCredentialRequestThrowsExceptionCredentialInvalidState()
        //{
        //    //Try double reject a credential request
        //}

        //TODO implement test
        //[Fact]
        //public async Task IssueCredentialThrowsExceptionCredentialNotFound()
        //{
            
        //}

        //TODO implement test
        //[Fact]
        //public async Task IssueCredentialThrowsExceptionCredentialInvalidState()
        //{
            
        //}

        //TODO implement test
        //[Fact]
        //public async Task IssueCredentialThrowsExceptionUnableToSendA2AMessage()
        //{

        //}

        //TODO implement test
        //[Fact]
        //public async Task RevokeCredentialThrowsExceptionCredentialNotFound()
        //{
            
        //}

        //TODO implement test
        //[Fact]
        //public async Task RevokeCredentialThrowsExceptionCredentialInvalidState()
        //{
            
        //}

        [Fact]
        public async Task RejectOfferAsyncThrowsExceptionCredentialOfferNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.RejectOfferAsync(_issuerWallet, "bad-credential-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        //TODO Implement test
        //[Fact]
        //public async Task RejectOfferAsyncThrowsExeceptionCredentialOfferInvalidState()
        //{
        //    //Try double reject a credential offer   
        //}

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(_issuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(_holderConfig, Credentials);
        }

        private async Task DisposePoolAsync(Pool pool)
        {
            if (pool != null) await pool.CloseAsync();
            await Pool.DeletePoolLedgerConfigAsync(_poolName);
        }
    }
}