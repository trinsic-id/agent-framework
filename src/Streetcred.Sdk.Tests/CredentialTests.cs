using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
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

        private readonly ISchemaService _schemaService;
        private readonly IPoolService _poolService;

        private readonly ConcurrentBag<IEnvelopeMessage> _messages = new ConcurrentBag<IEnvelopeMessage>();

        public CredentialTests()
        {
            var messageSerializer = new MessageSerializer();
            var recordService = new WalletRecordService();
            var ledgerService = new LedgerService();

            _poolService = new PoolService();

            var routingMock = new Mock<IRouterService>();
            routingMock.Setup(x => x.ForwardAsync(It.IsNotNull<IEnvelopeMessage>(), It.IsAny<AgentEndpoint>()))
                .Callback((IEnvelopeMessage content, AgentEndpoint endpoint) => { _messages.Add(content); })
                .Returns(Task.CompletedTask);

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult(new ProvisioningRecord
                {
                    Endpoint = new AgentEndpoint { Uri = MockEndpointUri },
                    MasterSecretId = MasterSecretId,
                    TailsBaseUri = MockEndpointUri
                }));

            var tailsService = new TailsService(ledgerService, provisioningMock.Object);
            _schemaService = new SchemaService(recordService, ledgerService, tailsService);

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
            catch (WalletExistsException)
            {
            }
            finally
            {
                _issuerWallet = await Wallet.OpenWalletAsync(IssuerConfig, Credentials);
                _holderWallet = await Wallet.OpenWalletAsync(HolderConfig, Credentials);
            }

            try
            {
                await _poolService.CreatePoolAsync(PoolName, Path.GetFullPath("pool_genesis.txn"), 2);
            }
            catch (PoolLedgerConfigExistsException)
            {
            }
            finally
            {
                _pool = await _poolService.GetPoolAsync(PoolName, 2);
            }
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
                _schemaService, _credentialService, _messages, issuerConnection.GetId(),
                _issuerWallet, _holderWallet, _pool, MasterSecretId);

            Assert.Equal(issuerCredential.State, holderCredential.State);
            Assert.Equal(CredentialState.Issued, issuerCredential.State);
        }

        public async Task DisposeAsync()
        {
            await _issuerWallet.CloseAsync();
            await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(IssuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(HolderConfig, Credentials);

            try
            {
                await _pool.CloseAsync();
            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
            catch (Exception)
            {
            }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
            await Pool.DeletePoolLedgerConfigAsync(PoolName);
        }
    }
}