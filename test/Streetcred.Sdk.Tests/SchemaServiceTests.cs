using System;
using System.IO;
using System.Threading.Tasks;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Runtime;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class SchemaServiceTests : IAsyncLifetime
    {
        private readonly IPoolService _poolService;
        private readonly ISchemaService _schemaService;

        private const string PoolName = "LedgerTestPool";
        private const string IssuerConfig = "{\"id\":\"issuer_credential_test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";

        private Pool _pool;
        private Wallet _issuerWallet;

        public SchemaServiceTests()
        {
            var walletService = new DefaultWalletRecordService();
            var ledgerService = new DefaultLedgerService();
            var tailsService = new DefaultTailsService(ledgerService);

            _poolService = new DefaultPoolService();
            _schemaService = new DefaultSchemaService(walletService, ledgerService, tailsService);
        }

        [Fact]
        public async Task CanCreateAndResolveSchema()
        {
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            var schemaName = $"Test-Schema-{Guid.NewGuid().ToString()}";
            var schemaVersion = "1.0";
            var schemaAttrNames = new[] {"test_attr_1", "test_attr_2"};

            //Create a dummy schema
            var schemaId = await _schemaService.CreateSchemaAsync(_pool, _issuerWallet, issuer.Did, schemaName, schemaVersion,
                schemaAttrNames);

            //Resolve it from the ledger with its identifier
            var resultSchema = await _schemaService.LookupSchemaAsync(_pool, _issuerWallet, issuer.Did, schemaId);

            var resultSchemaName = JObject.Parse(resultSchema)["name"].ToString();
            var resultSchemaVersion = JObject.Parse(resultSchema)["version"].ToString();
            var sequenceId = Convert.ToInt32(JObject.Parse(resultSchema)["seqNo"].ToString());

            Assert.Equal(schemaName, resultSchemaName);
            Assert.Equal(schemaVersion, resultSchemaVersion);

            //Resolve it from the ledger with its sequence Id
            var secondResultSchema = await _schemaService.LookupSchemaAsync(_pool, _issuerWallet, issuer.Did, sequenceId);

            var secondResultSchemaName = JObject.Parse(secondResultSchema)["name"].ToString();
            var secondResultSchemaVersion = JObject.Parse(secondResultSchema)["version"].ToString();

            Assert.Equal(schemaName, secondResultSchemaName);
            Assert.Equal(schemaVersion, secondResultSchemaVersion);
        }

        [Fact]
        public async Task CanCreateAndResolveCredentialDefinitionAndSchema()
        {
            var issuer = await Did.CreateAndStoreMyDidAsync(_issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            var schemaName = $"Test-Schema-{Guid.NewGuid().ToString()}";
            var schemaVersion = "1.0";
            var schemaAttrNames = new[] { "test_attr_1", "test_attr_2" };

            //Create a dummy schema
            var schemaId = await _schemaService.CreateSchemaAsync(_pool, _issuerWallet, issuer.Did, schemaName, schemaVersion,
                schemaAttrNames);

            var credId = await _schemaService.CreateCredentialDefinitionAsync(_pool, _issuerWallet, schemaId, issuer.Did, false, 100, new Uri("http://mock/tails"));

            var credDef =
                await _schemaService.LookupCredentialDefinitionAsync(_pool, _issuerWallet, issuer.Did, credId);

            var resultCredId = JObject.Parse(credDef)["id"].ToString();
            var schemaSequenceNo = JObject.Parse(credDef)["schemaId"];

            Assert.Equal(credId, resultCredId);

            var result = await _schemaService.LookupSchemaFromCredentialDefinitionAsync(_pool, _issuerWallet, issuer.Did, credId);

            var resultSchemaName = JObject.Parse(result)["name"].ToString();
            var resultSchemaVersion = JObject.Parse(result)["version"].ToString();

            Assert.Equal(schemaName, resultSchemaName);
            Assert.Equal(schemaVersion, resultSchemaVersion);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(IssuerConfig, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            _issuerWallet = await Wallet.OpenWalletAsync(IssuerConfig, Credentials);

            try
            {
                await _poolService.CreatePoolAsync(PoolName, Path.GetFullPath("pool_genesis.txn"), 2);
            }
            catch (PoolLedgerConfigExistsException)
            {
                // OK
            }
            _pool = await _poolService.GetPoolAsync(PoolName, 2);
        }

        public async Task DisposeAsync()
        {
            if (_issuerWallet != null) await _issuerWallet.CloseAsync();
            if (_pool != null) await _pool.CloseAsync();

            await Wallet.DeleteWalletAsync(IssuerConfig, Credentials);
            await Pool.DeletePoolLedgerConfigAsync(PoolName);
        }
    }
}
