using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Helpers;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
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

        private Pool _pool;

        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;

        private readonly ISchemaService _schemaService;
        private readonly IPoolService _poolService;

        private bool _routeMessage = true;
        private readonly ConcurrentBag<IAgentMessage> _messages = new ConcurrentBag<IAgentMessage>();

        public CredentialTests()
        {
            var messageSerializer = new DefaultMessageSerializer();
            var recordService = new DefaultWalletRecordService(new DateTimeHelper());
            var ledgerService = new DefaultLedgerService();

            _poolService = new DefaultPoolService();

            var routingMock = new Mock<IRouterService>();
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

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(() =>
                {
                    var provisioningRecord = new ProvisioningRecord();
                    provisioningRecord.Services.Add(new AgencyService() { ServiceEndpoint = MockEndpointUri });
                    return Task.FromResult(provisioningRecord);
                });

            var tailsService = new DefaultTailsService(ledgerService);
            _schemaService = new DefaultSchemaService(recordService, ledgerService, tailsService);

            _connectionService = new DefaultConnectionService(
                recordService,
                routingMock.Object,
                provisioningMock.Object,
                messageSerializer,
                new Mock<ILogger<DefaultConnectionService>>().Object);

            _credentialService = new DefaultCredentialService(
                routingMock.Object,
                ledgerService,
                _connectionService,
                recordService,
                messageSerializer,
                _schemaService,
                tailsService,
                provisioningMock.Object,
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

            try
            {
                await _poolService.CreatePoolAsync(_poolName, Path.GetFullPath("pool_genesis.txn"));
            }
            catch (PoolLedgerConfigExistsException)
            {
                // OK
            }
            _pool = await _poolService.GetPoolAsync(_poolName, 2);
        }

        /// <summary>
        /// This test requires a local running node accessible at 127.0.0.1
        /// </summary>
        /// <returns>The issuance demo.</returns>
        [Fact]
        public async Task CredentialIssuanceDemo()
        {
            // Setup secure connection between issuer and holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            var (issuerCredential, holderCredential) = await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, false);

            Assert.Equal(issuerCredential.State, holderCredential.State);
            Assert.Equal(CredentialState.Issued, issuerCredential.State);
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
            var (issuerConnection, _) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);
            
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            (var credId, _) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(_pool, _issuerWallet, _schemaService, issuer.Did,
                new[] { "dummy_attr" });

            _routeMessage = false;
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.SendOfferAsync(_issuerWallet, new OfferConfiguration
            {
                ConnectionId = issuerConnection.Id,
                CredentialDefinitionId = credId
            }));
            _routeMessage = true;

            Assert.True(ex.ErrorCode == ErrorCode.A2AMessageTransmissionError);
        }

        [Fact]
        public async Task ProcessCredentialRequestThrowsCredentialNotFound()
        {
            var (issuerConnection, _) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);
            
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.ProcessCredentialRequestAsync(_issuerWallet,
                new CredentialRequestMessage
                {
                    OfferJson = "{ \"nonce\":\"bad-nonce\" }"
                }, issuerConnection));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }
        
        [Fact]
        public async Task RejectCredentialRequestThrowsExceptionCredentialNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async() => await _credentialService.RejectCredentialRequestAsync(_holderWallet, "bad-credential-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task RejectCredentialRequestThrowsExceptionCredentialInvalidState()
        {
            //Establish a connection between the two parties
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            (var definitionId, _) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(_pool, _issuerWallet, _schemaService, issuer.Did,
                new[] { "dummy_attr" });

            var offerConfig = new OfferConfiguration()
            {
                ConnectionId = issuerConnection.Id,
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };

            // Send an offer to the holder using the established connection channel
            await _credentialService.SendOfferAsync(_issuerWallet, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(_messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await _credentialService.ProcessOfferAsync(_holderWallet, credentialOffer, holderConnection);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(_holderWallet, MasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            await _credentialService.AcceptOfferAsync(_holderWallet, _pool, holderCredentialId,
                new Dictionary<string, string>
                {
                    {"dummy_attr", "dummyVal"}
                });

            // Issuer retrieves credential request from cloud agent
            var credentialRequest = FindContentMessage<CredentialRequestMessage>(_messages);
            Assert.NotNull(credentialRequest);

            // Issuer processes the credential request by storing it
            var issuerCredentialId =
                await _credentialService.ProcessCredentialRequestAsync(_issuerWallet, credentialRequest, issuerConnection);

            await _credentialService.RejectCredentialRequestAsync(_issuerWallet, issuerCredentialId);

            //Try reject the credential request again
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.RejectCredentialRequestAsync(_issuerWallet, issuerCredentialId));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task IssueCredentialThrowsExceptionCredentialNotFound()
        {
            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());
            
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.IssueCredentialAsync(_pool, _issuerWallet, issuer.Did, "bad-credential-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task IssueCredentialThrowsExceptionCredentialInvalidState()
        {
            //Establish a connection between the two parties
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            (var definitionId, _) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(_pool, _issuerWallet, _schemaService, issuer.Did,
                new[] { "dummy_attr" });

            var offerConfig = new OfferConfiguration()
            {
                ConnectionId = issuerConnection.Id,
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };

            // Send an offer to the holder using the established connection channel
            await _credentialService.SendOfferAsync(_issuerWallet, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(_messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await _credentialService.ProcessOfferAsync(_holderWallet, credentialOffer, holderConnection);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(_holderWallet, MasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            await _credentialService.AcceptOfferAsync(_holderWallet, _pool, holderCredentialId,
                new Dictionary<string, string>
                {
                    {"dummy_attr", "dummyVal"}
                });

            // Issuer retrieves credential request from cloud agent
            var credentialRequest = FindContentMessage<CredentialRequestMessage>(_messages);
            Assert.NotNull(credentialRequest);

            // Issuer processes the credential request by storing it
            var issuerCredentialId =
                await _credentialService.ProcessCredentialRequestAsync(_issuerWallet, credentialRequest, issuerConnection);

            // Issuer accepts the credential requests and issues a credential
            await _credentialService.IssueCredentialAsync(_pool, _issuerWallet, issuer.Did, issuerCredentialId);

            //Try issue the credential again
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.IssueCredentialAsync(_pool, _issuerWallet, issuer.Did, issuerCredentialId));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task IssueCredentialThrowsExceptionUnableToSendA2AMessage()
        {
            //Establish a connection between the two parties
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            (var definitionId, _) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(_pool, _issuerWallet, _schemaService, issuer.Did,
                new[] { "dummy_attr" });

            var offerConfig = new OfferConfiguration()
            {
                ConnectionId = issuerConnection.Id,
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };

            // Send an offer to the holder using the established connection channel
            await _credentialService.SendOfferAsync(_issuerWallet, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(_messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await _credentialService.ProcessOfferAsync(_holderWallet, credentialOffer, holderConnection);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(_holderWallet, MasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            await _credentialService.AcceptOfferAsync(_holderWallet, _pool, holderCredentialId,
                new Dictionary<string, string>
                {
                    {"dummy_attr", "dummyVal"}
                });

            // Issuer retrieves credential request from cloud agent
            var credentialRequest = FindContentMessage<CredentialRequestMessage>(_messages);
            Assert.NotNull(credentialRequest);

            // Issuer processes the credential request by storing it
            var issuerCredentialId =
                await _credentialService.ProcessCredentialRequestAsync(_issuerWallet, credentialRequest, issuerConnection);

            //Try issue the credential with a credential service that has a bad routing service

            _routeMessage = false;
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.IssueCredentialAsync(_pool, _issuerWallet, issuer.Did, issuerCredentialId));
            Assert.True(ex.ErrorCode == ErrorCode.A2AMessageTransmissionError);
            _routeMessage = true;
        }
        
        [Fact]
        public async Task RejectOfferAsyncThrowsExceptionCredentialOfferNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.RejectOfferAsync(_issuerWallet, "bad-credential-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task RejectOfferAsyncThrowsExeceptionCredentialOfferInvalidState()
        {
            //Establish a connection between the two parties
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            (var definitionId, _) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(_pool, _issuerWallet, _schemaService, issuer.Did,
                new[] { "dummy_attr" });

            var offerConfig = new OfferConfiguration()
            {
                ConnectionId = issuerConnection.Id,
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };

            // Send an offer to the holder using the established connection channel
            await _credentialService.SendOfferAsync(_issuerWallet, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(_messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await _credentialService.ProcessOfferAsync(_holderWallet, credentialOffer, holderConnection);

            //Reject the offer
            await _credentialService.RejectOfferAsync(_holderWallet, holderCredentialId);

            //Try reject the offer again
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _credentialService.RejectOfferAsync(_holderWallet, holderCredentialId));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        private static T FindContentMessage<T>(IEnumerable<IAgentMessage> collection)
            where T : IAgentMessage
            => collection.OfType<T>().Single();

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.CloseAsync();
            if (_pool != null) await _pool.CloseAsync();

            await Wallet.DeleteWalletAsync(_issuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(_holderConfig, Credentials);
            await Pool.DeletePoolLedgerConfigAsync(_poolName);
        }
    }
}