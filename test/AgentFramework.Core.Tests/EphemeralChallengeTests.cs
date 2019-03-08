using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.EphemeralChallenge;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.EphemeralChallenge;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class EphemeralChallengeTests : IAsyncLifetime
    {
        private readonly string _poolName = $"Pool{Guid.NewGuid()}";
        private readonly string _issuerConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private readonly string _holderConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private readonly string _requestorConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private const string MockEndpointUri = "http://mock";
        private const string MasterSecretId = "DefaultMasterSecret";
        
        private IAgentContext _issuerWallet;
        private IAgentContext _holderWallet;
        private IAgentContext _requestorWallet;

        private Pool _pool;

        private readonly IEventAggregator _eventAggregator;
        private readonly IConnectionService _connectionService;
        private readonly IProofService _proofService;
        private readonly ICredentialService _credentialService;
        private readonly IEphemeralChallengeService _ephemeralChallengeService;

        private readonly ISchemaService _schemaService;
        private readonly IPoolService _poolService;

        private bool _routeMessage = true;
        private readonly ConcurrentBag<AgentMessage> _messages = new ConcurrentBag<AgentMessage>();

        public EphemeralChallengeTests()
        {
            var recordService = new DefaultWalletRecordService();
            var ledgerService = new DefaultLedgerService();

            _eventAggregator = new EventAggregator();
            _poolService = new DefaultPoolService();

            var routingMock = new Mock<IMessageService>();
            routingMock.Setup(x =>
                    x.SendToConnectionAsync(It.IsAny<Wallet>(), It.IsAny<AgentMessage>(), It.IsAny<ConnectionRecord>(), It.IsAny<string>()))
                .Callback((Wallet _, AgentMessage content, ConnectionRecord __, string ___) =>
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
                    Endpoint = new AgentEndpoint { Uri = MockEndpointUri },
                    MasterSecretId = MasterSecretId
                }));

            var tailsService = new DefaultTailsService(ledgerService, new HttpClientHandler());

            _schemaService = new DefaultSchemaService(provisioningMock.Object, recordService, ledgerService, tailsService);

            _connectionService = new DefaultConnectionService(
                _eventAggregator,
                recordService,
                provisioningMock.Object,
                new Mock<ILogger<DefaultConnectionService>>().Object);

            _credentialService = new DefaultCredentialService(
                _eventAggregator,
                ledgerService,
                _connectionService,
                recordService,
                _schemaService,
                tailsService,
                provisioningMock.Object,
                new Mock<ILogger<DefaultCredentialService>>().Object);

            _proofService = new DefaultProofService(
                _eventAggregator,
                _connectionService,
                recordService,
                provisioningMock.Object,
                ledgerService,
                tailsService,
                new Mock<ILogger<DefaultProofService>>().Object);

            _ephemeralChallengeService = new DefaultEphemeralChallengeService(_eventAggregator, _proofService, recordService, new Mock<ILogger<DefaultEphemeralChallengeService>>().Object);

        }

        public async Task InitializeAsync()
        {
            try
            {
                await _poolService.CreatePoolAsync(_poolName, Path.GetFullPath("pool_genesis.txn"));
            }
            catch (PoolLedgerConfigExistsException)
            {
                // OK
            }

            _pool = await _poolService.GetPoolAsync(_poolName, 2);

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

            try
            {
                await Wallet.CreateWalletAsync(_requestorConfig, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            _issuerWallet = new AgentContext
            {
                Wallet = await Wallet.OpenWalletAsync(_issuerConfig, Credentials), 
                Pool = _pool
            };
            _holderWallet = new AgentContext
            {
                Wallet = await Wallet.OpenWalletAsync(_holderConfig, Credentials), 
                Pool = _pool
            };
            _requestorWallet = new AgentContext
            {
                Wallet = await Wallet.OpenWalletAsync(_requestorConfig, Credentials),
                Pool = _pool
            };
        }

        [Fact]
        public async Task CanCreateChallengeConfigAsync()
        {
            var config = new EphemeralChallengeConfiguration
            {
                Name = "Test",
                Type = ChallengeType.Proof,
                Contents = new ProofRequestConfiguration
                {
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"", new ProofAttributeInfo {Name = "Test"}}
                    }
                }
            };

            var id = await _ephemeralChallengeService.CreateChallengeConfigAsync(_issuerWallet, config);

            var record = await _ephemeralChallengeService.GetChallengeConfigAsync(_issuerWallet, id);

            var result = record.Contents.ToObject<ProofRequestConfiguration>();

            Assert.True(result.RequestedAttributes.Count == 1);
            Assert.True(config.Type == record.Type);
            Assert.True(config.Name == record.Name);
        }

        [Fact]
        public async Task GetChallengeConfigAsyncThrowsRecordNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () =>
                await _ephemeralChallengeService.GetChallengeConfigAsync(_holderWallet, "bad-config-id"));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task GetChallengeAsyncThrowsRecordNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () =>
                await _ephemeralChallengeService.GetChallengeAsync(_holderWallet, "bad-config-id"));

            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task CanCreateChallengeAsync()
        {
            var config = new EphemeralChallengeConfiguration
            {
                Name = "Test",
                Type = ChallengeType.Proof,
                Contents = new ProofRequestConfiguration
                {
                    RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                    {
                        {"", new ProofAttributeInfo {Name = "Test"}}
                    }
                }
            };

            var id = await _ephemeralChallengeService.CreateChallengeConfigAsync(_holderWallet, config);

            var result = await _ephemeralChallengeService.CreateChallengeAsync(_holderWallet, id);

            Assert.True(!string.IsNullOrEmpty(result.ChallengeId));
            Assert.True(result.Challenge != null);
        }

        [Fact]
        public async Task CanConductChallengeFlow()
        {
            //Setup a connection and issue the credentials to the holder
            var (issuerConnection, holderConnection) = await Scenarios.EstablishConnectionAsync(
                _connectionService, _messages, _issuerWallet, _holderWallet);

            await Scenarios.IssueCredentialAsync(
                _schemaService, _credentialService, _messages, issuerConnection,
                holderConnection, _issuerWallet, _holderWallet, _pool, MasterSecretId, true);

            _messages.Clear();

            // Challenger sends a challenge
            {
                var challengeConfig = new EphemeralChallengeConfiguration
                {
                    Name = "Test",
                    Type = ChallengeType.Proof,
                    Contents = new ProofRequestConfiguration
                    {
                        RequestedAttributes = new Dictionary<string, ProofAttributeInfo>
                        {
                            {"first-name-requirement", new ProofAttributeInfo {Name = "first_name"}}
                        }
                    }
                };

                var challengeConfigId = await _ephemeralChallengeService.CreateChallengeConfigAsync(_requestorWallet, challengeConfig);

                var challengeResult = await _ephemeralChallengeService.CreateChallengeAsync(_requestorWallet, challengeConfigId);
                _messages.Add(challengeResult.Challenge);

                var result = await _ephemeralChallengeService.GetChallengeState(_requestorWallet, challengeResult.ChallengeId);
                Assert.True(result == ChallengeState.Challenged);
            }

            //Challenge responder recieves challenge
            {
                var challengeMessage = _messages.OfType<EphemeralChallengeMessage>().First();

                var proofRequest = challengeMessage.Challenge.Contents.ToObject<ProofRequest>();

                var requestedCredentials = new RequestedCredentials();
                foreach (var requestedAttribute in proofRequest.RequestedAttributes)
                {
                    var credentials =
                        await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, proofRequest,
                            requestedAttribute.Key);

                    requestedCredentials.RequestedAttributes.Add(requestedAttribute.Key,
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                }

                foreach (var requestedAttribute in proofRequest.RequestedPredicates)
                {
                    var credentials =
                        await _proofService.ListCredentialsForProofRequestAsync(_holderWallet, proofRequest,
                            requestedAttribute.Key);

                    requestedCredentials.RequestedPredicates.Add(requestedAttribute.Key,
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true
                        });
                }

                _messages.Add(await _ephemeralChallengeService.AcceptChallenge(_holderWallet, challengeMessage, requestedCredentials));
            }

            //Challenger recieves challenge response and verifies it
            {
                var challengeResponseMessage = _messages.OfType<EphemeralChallengeResponseMessage>().First();

                var id = await _ephemeralChallengeService.ProcessChallengeResponseAsync(_requestorWallet, challengeResponseMessage);

                var result = await _ephemeralChallengeService.GetChallengeState(_requestorWallet, id);
                Assert.True(result == ChallengeState.Accepted);
            }
        }

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.Wallet.CloseAsync();
            if (_holderWallet != null) await _holderWallet.Wallet.CloseAsync();
            if (_requestorWallet != null) await _requestorWallet.Wallet.CloseAsync();
            if (_pool != null) await _pool.CloseAsync();

            await Wallet.DeleteWalletAsync(_issuerConfig, Credentials);
            await Wallet.DeleteWalletAsync(_holderConfig, Credentials);
            await Wallet.DeleteWalletAsync(_requestorConfig, Credentials);
            await Pool.DeletePoolLedgerConfigAsync(_poolName);
        }
    }
}