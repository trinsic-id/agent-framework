using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class ProofTests : IAsyncLifetime
    {
        private readonly string _poolName = $"Pool{Guid.NewGuid()}";
        private const string IssuerConfig = "{\"id\":\"issuer_proof_test_wallet\"}";
        private const string HolderConfig = "{\"id\":\"holder_proof_test_wallet\"}";
        private const string RequestorConfig = "{\"id\":\"requestor_proof_test_wallet\"}";
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

        private bool _routeMessage = true;
        private readonly ConcurrentBag<IAgentMessage> _messages = new ConcurrentBag<IAgentMessage>();

        public ProofTests()
        {
            var messageSerializer = new DefaultMessageSerializer();
            var recordService = new DefaultWalletRecordService();
            var ledgerService = new DefaultLedgerService();

            _poolService = new DefaultPoolService();

            var provisionMock = new Mock<IProvisioningService>();
            provisionMock.Setup(x => x.GetProvisioningAsync(It.IsAny<Wallet>()))
                .Returns(
                    Task.FromResult<ProvisioningRecord>(new ProvisioningRecord() {MasterSecretId = MasterSecretId}));

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
                .Returns(Task.FromResult(new ProvisioningRecord
                {
                    Endpoint = new AgentEndpoint {Uri = MockEndpointUri},
                    MasterSecretId = MasterSecretId
                }));

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

            _proofService = new DefaultProofService(
                _connectionService,
                routingMock.Object,
                messageSerializer,
                recordService,
                provisionMock.Object,
                ledgerService,
                tailsService,
                new Mock<ILogger<DefaultProofService>>().Object);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(IssuerConfig, WalletCredentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            try
            {
                await Wallet.CreateWalletAsync(HolderConfig, WalletCredentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            try
            {
                await Wallet.CreateWalletAsync(RequestorConfig, WalletCredentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }
            
            _issuerWallet = await Wallet.OpenWalletAsync(IssuerConfig, WalletCredentials);
            _holderWallet = await Wallet.OpenWalletAsync(HolderConfig, WalletCredentials);
            _requestorWallet = await Wallet.OpenWalletAsync(RequestorConfig, WalletCredentials);

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

        [Fact]
        public async Task CredentialProofDemo()
        { 
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection, 
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, true);

            _messages.Clear();

            //Requestor initialize a connection with the holder
            var (_, requestorConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _holderWallet, _requestorWallet);

            // Verifier sends a proof request to prover
            {
                var proofRequestObject = new ProofRequest
                {
                    Name = "ProofReq",
                    Version = "1.0",
                    Nonce = "123",
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"first-name-requirement", new ProofAttributeInfo {Name = "first_name"}}
                    }
                };

                //Requestor sends a proof request
                await _proofService.SendProofRequestAsync(_requestorWallet, requestorConnection.Id, proofRequestObject);
            }

            // Holder accepts the proof requests and builds a proof
            {
                //Holder retrives proof request message from their cloud agent
                var proofRequest = FindContentMessage<ProofRequestMessage>();
                Assert.NotNull(proofRequest);

                //Holder stores the proof request
                var holderProofRequestId = await _proofService.ProcessProofRequestAsync(_holderWallet, proofRequest, holderConnection);
                var holderProofRecord = await _proofService.GetAsync(_holderWallet, holderProofRequestId);
                var holderProofObject =
                    JsonConvert.DeserializeObject<ProofRequest>(holderProofRecord.RequestJson);

                var requestedCredentials = new RequestedCredentials();
                foreach (var requestedAttribute in holderProofObject.RequestedAttributes)
                {
                    var credentials =
                        await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, holderProofObject,
                            requestedAttribute.Key);

                    requestedCredentials.RequestedAttributes.Add(requestedAttribute.Key,
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                }

                foreach (var requestedAttribute in holderProofObject.RequestedPredicates)
                {
                    var credentials =
                        await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, holderProofObject,
                            requestedAttribute.Key);

                    requestedCredentials.RequestedPredicates.Add(requestedAttribute.Key,
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true
                        });
                }

                //Holder accepts the proof request and sends a proof
                await _proofService.AcceptProofRequestAsync(_holderWallet, _pool, holderProofRequestId,
                    requestedCredentials);
            }

            //Requestor retrives proof message from their cloud agent
            var proof = FindContentMessage<ProofMessage>();
            Assert.NotNull(proof);

            //Requestor stores proof
            var requestorProofId = await _proofService.ProcessProofAsync(_requestorWallet, proof, requestorConnection);

            //Requestor verifies proof
            var requestorVerifyResult = await _proofService.VerifyProofAsync(_requestorWallet, _pool, requestorProofId);

            ////Verify the proof is valid
            Assert.True(requestorVerifyResult);

            ////Get the proof from both parties wallets
            //var requestorProof = await _proofService.GetProof(_requestorWallet, requestorProofId);
            //var holderProof = await _proofService.GetProof(_holderWallet, holderProofRequestId);

            ////Verify that both parties have a copy of the proof
            //Assert.Equal(requestorProof, holderProof);
        }

        [Fact]
        public async Task SendProofRequestThrowsConnectionNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.SendProofRequestAsync(_issuerWallet, "bad-proof-id", new ProofRequest
            {
                Name = "Test"
            }));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task SendProofRequestThrowsConnectionInvalidState()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId });

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.SendProofRequestAsync(_issuerWallet, connectionId, new ProofRequest
            {
                Name = "Test"
            }));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }
        
        [Fact]
        public async Task SendProofRequestThrowsA2AMessageTransmissionFailure()
        {
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, true);

            _messages.Clear();

            //Requestor initialize a connection with the holder
            var (_, requestorConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _holderWallet, _requestorWallet);

            // Verifier sends a proof request to prover
            {
                var proofRequestObject = new ProofRequest
                {
                    Name = "ProofReq",
                    Version = "1.0",
                    Nonce = "123",
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"first-name-requirement", new ProofAttributeInfo {Name = "first_name"}}
                    }
                };

                //Requestor sends a proof request
                _routeMessage = false;
                var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.SendProofRequestAsync(_requestorWallet, requestorConnection.Id, proofRequestObject));
                _routeMessage = true;

                Assert.True(ex.ErrorCode == ErrorCode.A2AMessageTransmissionError);
            }
        }

        [Fact]
        public async Task CreateProofRequestConnectionNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.CreateProofRequestAsync(_issuerWallet, "bad-connection-id", new ProofRequest
            {
                Name = "Test",
                Nonce = "1234"
            }));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task CreateProofRequestConnectionInvalidState()
        {
            var connectionId = Guid.NewGuid().ToString();

            await _connectionService.CreateInvitationAsync(_issuerWallet,
                new InviteConfiguration() { ConnectionId = connectionId });

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.CreateProofRequestAsync(_issuerWallet, connectionId, new ProofRequest
            {
                Name = "Test",
                Nonce = "1234"
            }));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task ProcessProofRecordNotFound()
        {
            var (issuerConnection, _) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () =>
                await _proofService.ProcessProofAsync(_issuerWallet, new ProofMessage
                {
                    RequestNonce = "123"
                }, issuerConnection));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task ProcessProofInvalidState()
        {
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, true);

            _messages.Clear();

            //Requestor initialize a connection with the holder
            var (_, requestorConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _holderWallet, _requestorWallet);

            // Verifier sends a proof request to prover
            {
                var proofRequestObject = new ProofRequest
                {
                    Name = "ProofReq",
                    Version = "1.0",
                    Nonce = "123",
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"first-name-requirement", new ProofAttributeInfo {Name = "first_name"}}
                    }
                };

                //Requestor sends a proof request
                await _proofService.SendProofRequestAsync(_requestorWallet, requestorConnection.Id, proofRequestObject);
            }

            // Holder accepts the proof requests and builds a proof
            {
                //Holder retrives proof request message from their cloud agent
                var proofRequest = FindContentMessage<ProofRequestMessage>();
                Assert.NotNull(proofRequest);

                //Holder stores the proof request
                var holderProofRequestId = await _proofService.ProcessProofRequestAsync(_holderWallet, proofRequest, holderConnection);
                var holderProofRecord = await _proofService.GetAsync(_holderWallet, holderProofRequestId);
                var holderProofObject =
                    JsonConvert.DeserializeObject<ProofRequest>(holderProofRecord.RequestJson);

                var requestedCredentials = new RequestedCredentials();
                foreach (var requestedAttribute in holderProofObject.RequestedAttributes)
                {
                    var credentials =
                        await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, holderProofObject,
                            requestedAttribute.Key);

                    requestedCredentials.RequestedAttributes.Add(requestedAttribute.Key,
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                }

                foreach (var requestedAttribute in holderProofObject.RequestedPredicates)
                {
                    var credentials =
                        await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, holderProofObject,
                            requestedAttribute.Key);

                    requestedCredentials.RequestedPredicates.Add(requestedAttribute.Key,
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true
                        });
                }

                //Holder accepts the proof request and sends a proof
                await _proofService.AcceptProofRequestAsync(_holderWallet, _pool, holderProofRequestId,
                    requestedCredentials);
            }

            //Requestor retrives proof message from their cloud agent
            var proof = FindContentMessage<ProofMessage>();
            Assert.NotNull(proof);

            //Requestor stores proof
            await _proofService.ProcessProofAsync(_requestorWallet, proof, requestorConnection);

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.ProcessProofAsync(_requestorWallet, proof, requestorConnection));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task AcceptProofRequestCredentialNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.AcceptProofRequestAsync(_issuerWallet, _pool, "bad-proof-id", null));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task AcceptProofRequestCredentialInvalidState()
        {
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, true);

            _messages.Clear();

            //Requestor initialize a connection with the holder
            var (_, requestorConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _holderWallet, _requestorWallet);

            // Verifier sends a proof request to prover
            {
                var proofRequestObject = new ProofRequest
                {
                    Name = "ProofReq",
                    Version = "1.0",
                    Nonce = "123",
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"first-name-requirement", new ProofAttributeInfo {Name = "first_name"}}
                    }
                };

                //Requestor sends a proof request
                await _proofService.SendProofRequestAsync(_requestorWallet, requestorConnection.Id, proofRequestObject);
            }

            // Holder accepts the proof requests and builds a proof
            //Holder retrives proof request message from their cloud agent
            var proofRequest = FindContentMessage<ProofRequestMessage>();
            Assert.NotNull(proofRequest);

            //Holder stores the proof request
            var holderProofRequestId = await _proofService.ProcessProofRequestAsync(_holderWallet, proofRequest, holderConnection);
            var holderProofRecord = await _proofService.GetAsync(_holderWallet, holderProofRequestId);
            var holderProofObject =
                JsonConvert.DeserializeObject<ProofRequest>(holderProofRecord.RequestJson);

            var requestedCredentials = new RequestedCredentials();
            foreach (var requestedAttribute in holderProofObject.RequestedAttributes)
            {
                var credentials =
                    await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, holderProofObject,
                        requestedAttribute.Key);

                requestedCredentials.RequestedAttributes.Add(requestedAttribute.Key,
                    new RequestedAttribute
                    {
                        CredentialId = credentials.First().CredentialInfo.Referent,
                        Revealed = true,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
            }

            foreach (var requestedAttribute in holderProofObject.RequestedPredicates)
            {
                var credentials =
                    await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, holderProofObject,
                        requestedAttribute.Key);

                requestedCredentials.RequestedPredicates.Add(requestedAttribute.Key,
                    new RequestedAttribute
                    {
                        CredentialId = credentials.First().CredentialInfo.Referent,
                        Revealed = true
                    });
            }

            //Holder accepts the proof request and sends a proof
            await _proofService.AcceptProofRequestAsync(_holderWallet, _pool, holderProofRequestId,
                requestedCredentials);
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.AcceptProofRequestAsync(_holderWallet, _pool, holderProofRequestId,
                requestedCredentials));

            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        [Fact]
        public async Task RejectProofRequestCredentialNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.RejectProofRequestAsync(_issuerWallet, "bad-proof-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task RejectProofRequestCredentialInvalidState()
        {
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, true);

            _messages.Clear();

            //Requestor initialize a connection with the holder
            var (_, requestorConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _holderWallet, _requestorWallet);

            // Verifier sends a proof request to prover
            {
                var proofRequestObject = new ProofRequest
                {
                    Name = "ProofReq",
                    Version = "1.0",
                    Nonce = "123",
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"first-name-requirement", new ProofAttributeInfo {Name = "first_name"}}
                    }
                };

                //Requestor sends a proof request
                await _proofService.SendProofRequestAsync(_requestorWallet, requestorConnection.Id, proofRequestObject);
            }
            
            //Holder retrives proof request message from their cloud agent
            var proofRequest = FindContentMessage<ProofRequestMessage>();
            Assert.NotNull(proofRequest);

            //Holder stores the proof request
            var holderProofRequestId = await _proofService.ProcessProofRequestAsync(_holderWallet, proofRequest, holderConnection);

            //Holder accepts the proof request and sends a proof
            await _proofService.RejectProofRequestAsync(_holderWallet, holderProofRequestId);

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _proofService.RejectProofRequestAsync(_holderWallet, holderProofRequestId));
            Assert.True(ex.ErrorCode == ErrorCode.RecordInInvalidState);
        }

        private T FindContentMessage<T>() where T : IAgentMessage
            => _messages.OfType<T>().Single();

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.CloseAsync();
            if (_requestorWallet != null) await _requestorWallet.CloseAsync();
            if (_pool != null) await _pool.CloseAsync();

            await Wallet.DeleteWalletAsync(IssuerConfig, WalletCredentials);
            await Wallet.DeleteWalletAsync(HolderConfig, WalletCredentials);
            await Wallet.DeleteWalletAsync(RequestorConfig, WalletCredentials);
            await Pool.DeletePoolLedgerConfigAsync(_poolName);
        }
    }
}