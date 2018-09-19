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
                .Returns(Task.FromResult<ProvisioningRecord>(new ProvisioningRecord() {MasterSecretId = MasterSecretId}));

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
            await IssueCredentialsAsync();

            //Requestor initialize a connection with the holder
            var (holderConnection, requestorConnection) = await EstablishConnectionAsync(_holderWallet, _requestorWallet);

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
            await _proofService.SendProofRequestAsync(requestorConnection.ConnectionId, _holderWallet, proofRequestObject);

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

        private async Task<(ConnectionRecord connectionParty1, ConnectionRecord connectionParty2)> EstablishConnectionAsync(Wallet party1, Wallet party2)
        {
            // Create invitation by the party1
            var party1ConnectionId = Guid.NewGuid().ToString();
            var invitation = await _connectionService.CreateInvitationAsync(party1,
                new CreateInviteConfiguration() { ConnectionId = party1ConnectionId });
            var connectionParty1 = await _connectionService.GetAsync(party1, party1ConnectionId);

            // Party2 accepts invitation and sends a message request
            var party2ConnectionId = await _connectionService.AcceptInvitationAsync(party2, invitation);
            var connectionParty2 = await _connectionService.GetAsync(party2, party2ConnectionId);

            // Party1 processes incoming message
            var party1Message = _messages.OfType<ForwardToKeyEnvelopeMessage>()
                .First(x => x.Type.Contains(connectionParty1.Tags.Single(item => item.Key == "connectionKey").Value));

            var requestMessage = GetContentMessage(party1Message) as ConnectionRequest;
            Assert.NotNull(requestMessage);

            // Party1 stores and accepts the request
            await _connectionService.StoreRequestAsync(party1, requestMessage);
            await _connectionService.AcceptRequestAsync(party1, party1ConnectionId);

            // Party2 processes incoming message
            var party2Message = _messages.OfType<ForwardEnvelopeMessage>()
                .First(x => x.Type.Contains(connectionParty2.MyDid));

            var responseMessage = GetContentMessage(party2Message) as ConnectionResponse;
            Assert.NotNull(responseMessage);

            // Party2 accepts response message
            await _connectionService.AcceptResponseAsync(party2, responseMessage);

            // Retrieve updated connection state for both party1 and party2
            connectionParty1 = await _connectionService.GetAsync(party1, party1ConnectionId);
            connectionParty2 = await _connectionService.GetAsync(party2, party2ConnectionId);

            return (connectionParty1, connectionParty2);
        }

        private async Task IssueCredentialsAsync()
        {
            // Setup secure connection between issuer and holder
            var (issuerConnection, holderConnection) = await EstablishConnectionAsync(_issuerWallet, _holderWallet);

            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            var schemaId = await _schemaService.CreateSchemaAsync(_pool, _issuerWallet, issuer.Did,
                $"Test-Schema-{Guid.NewGuid().ToString()}", "1.0", new[] { "first_name", "last_name" });
            var definitionId =
                await _schemaService.CreateCredentialDefinitionAsync(_pool, _issuerWallet, schemaId, issuer.Did, true,
                    100);

            // Send an offer to the holder using the established connection channel
            await _credentialService.SendOfferAsync(definitionId, issuerConnection.GetId(), _issuerWallet, issuer.Did);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOffer>();

            // Holder stores the credential offer
            var holderCredentialId =
                await _credentialService.StoreOfferAsync(_holderWallet, credentialOffer);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(_holderWallet, MasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            await _credentialService.AcceptOfferAsync(_holderWallet, _pool, holderCredentialId,
                new Dictionary<string, string>
                {
                    {"first_name", "Jane"},
                    {"last_name", "Doe"}
                });

            // Issuer retrieves credential request from cloud agent
            var credentialRequest = FindContentMessage<CredentialRequest>();
            Assert.NotNull(credentialRequest);

            // Issuer stores the credential request
            var issuerCredentialId =
                await _credentialService.StoreCredentialRequestAsync(_issuerWallet, credentialRequest);

            // Issuer accepts the credential requests and issues a credential
            await _credentialService.IssueCredentialAsync(_pool, _issuerWallet, issuer.Did, issuerCredentialId);

            // Holder retrieves the credential from their cloud agent
            var credential = FindContentMessage<Credential>();
            Assert.NotNull(credential);

            // Holder stores the credential in their wallet
            await _credentialService.StoreCredentialAsync(_pool, _holderWallet, credential);

            // Verify states of both credential records are set to 'Issued'
            var issuerCredential = await _credentialService.GetAsync(_issuerWallet, issuerCredentialId);
            var holderCredential = await _credentialService.GetAsync(_holderWallet, holderCredentialId);

            Assert.Equal(issuerCredential.State, holderCredential.State);
            Assert.Equal(CredentialState.Issued, issuerCredential.State);
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
