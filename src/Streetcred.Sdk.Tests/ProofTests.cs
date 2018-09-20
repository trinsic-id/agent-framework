using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Proofs;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Wallets;
using Streetcred.Sdk.Runtime;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class ProofTests : IAsyncLifetime
    {
        private const string PoolName = "CredentialTestPool";
        private const string IssuerConfig = "{\"id\":\"issuer_credential_test_wallet\"}";
        private const string HolderConfig = "{\"id\":\"holder_credential_test_wallet\"}";
        private const string RequestorConfig = "{\"id\":\"requestor_credential_test_wallet\"}";
        private const string WalletCredentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";
        private const string MasterSecretId = "DefaultMasterSecret";

        private Pool _pool;
        private Wallet _issuerWallet;
        private Wallet _holderWallet;
        private Wallet _requestorWallet;

        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;
        private readonly IProofService _proofService;

        private readonly ISchemaService _schemaService;
        private readonly IPoolService _poolService;

        private readonly ConcurrentBag<IEnvelopeMessage> _messages = new ConcurrentBag<IEnvelopeMessage>();

        public ProofTests()
        {
            var messageSerializer = new MessageSerializer();
            var recordService = new WalletRecordService();
            var ledgerService = new LedgerService();
            var tailsService = new TailsService();

            _poolService = new PoolService();
            _schemaService = new SchemaService(recordService, ledgerService, tailsService);

            var provisionMock = new Mock<IProvisioningService>();
            provisionMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult<ProvisioningRecord>(new ProvisioningRecord() { MasterSecretId = MasterSecretId }));

            var routingMock = new Mock<IRouterService>();
            routingMock.Setup(x => x.ForwardAsync(It.IsNotNull<IEnvelopeMessage>(), It.IsAny<AgentEndpoint>()))
                .Callback((IEnvelopeMessage content, AgentEndpoint endpoint) => { _messages.Add(content); })
                .Returns(Task.CompletedTask);

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(Task.FromResult(new ProvisioningRecord
                {
                    Endpoint = new AgentEndpoint { Uri = MockEndpointUri },
                    MasterSecretId = MasterSecretId
                }));

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

            _proofService = new ProofService(
                _connectionService,
                routingMock.Object,
                messageSerializer,
                recordService,
                provisionMock.Object,
                _schemaService,
                ledgerService,
                _credentialService,
                new Mock<ILogger<ProofService>>().Object);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(IssuerConfig, WalletCredentials);
                await Wallet.CreateWalletAsync(HolderConfig, WalletCredentials);
                await Wallet.CreateWalletAsync(RequestorConfig, WalletCredentials);
            }
            catch (WalletExistsException)
            {
            }
            finally
            {
                _issuerWallet = await Wallet.OpenWalletAsync(IssuerConfig, WalletCredentials);
                _holderWallet = await Wallet.OpenWalletAsync(HolderConfig, WalletCredentials);
                _requestorWallet = await Wallet.OpenWalletAsync(RequestorConfig, WalletCredentials);
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

        [Fact]
        public async Task CredentialProofDemo()
        {
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, _) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            var (_, _) = await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection.GetId(),
                _issuerWallet, _holderWallet, _pool, MasterSecretId);

            _messages.Clear();

            //Requestor initialize a connection with the holder
            var (holderRequestorConnection, requestorConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _holderWallet, _requestorWallet);

            var proofRequestObject = new ProofRequestObject
            {
                Name = "ProofReq",
                Version = "1.0",
                RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                {
                    {"proof.first_name", new ProofAttributeInfo {Name = "first_name"}},
                    {"proof.last_name", new ProofAttributeInfo {Name = "last_name"}}
                }
            };

            //Requestor sends a proof request
            await _proofService.SendProofRequestAsync(requestorConnection.ConnectionId, _requestorWallet, proofRequestObject);

            //Holder retrives proof request message from their cloud agent
            var proofRequest = FindContentMessage<ProofRequest>();
            Assert.NotNull(proofRequest);

            //Holder stores the proof request
            var holderProofRequestId = await _proofService.StoreProofRequestAsync(_holderWallet, proofRequest);

            //Holder accepts the proof request and sends a proof
            await _proofService.AcceptProofRequestAsync(_holderWallet, _pool, holderProofRequestId);

            //Requestor retrives proof message from their cloud agent
            var proof = FindContentMessage<Proof>();
            Assert.NotNull(proof);

            //Requestor stores proof
            var requestorProofId =
                await _proofService.StoreProofAsync(_requestorWallet, proof);

            //Requestor verifies proof
            var requestorVerifyResult = await _proofService.VerifyProofAsync(_requestorWallet, _pool, requestorProofId);

            //Verify the proof is valid
            Assert.True(requestorVerifyResult);

            //Get the proof from both parties wallets
            var requestorProof = await _proofService.GetProof(_requestorWallet, requestorProofId);
            var holderProof = await _proofService.GetProof(_holderWallet, holderProofRequestId);

            //Verify that both parties have a copy of the proof
            Assert.Equal(requestorProof, holderProof);
        }


        private IContentMessage GetContentMessage(IEnvelopeMessage message)
            => JsonConvert.DeserializeObject<IContentMessage>(message.Content);

        private T FindContentMessage<T>() where T : IContentMessage
            => _messages.Select(GetContentMessage).OfType<T>().Single();

        public async Task DisposeAsync()
        {
            await _issuerWallet.CloseAsync();
            await _holderWallet.CloseAsync();

            await Wallet.DeleteWalletAsync(IssuerConfig, WalletCredentials);
            await Wallet.DeleteWalletAsync(HolderConfig, WalletCredentials);

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
