using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
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
            var resultSchemaAttrNames = JObject.Parse(resultSchema)["attrNames"];
            var sequenceId = Convert.ToInt32(JObject.Parse(resultSchema)["seqNo"].ToString());

            Assert.Equal(schemaName, resultSchemaName);
            Assert.Equal(schemaVersion, resultSchemaVersion);
            //Assert.Equal(schemaAttrNames, resultSchemaAttrNames);

            //Resolve it from the ledger with its sequence Id
            var secondResultSchema = await _schemaService.LookupSchemaAsync(_pool, _issuerWallet, issuer.Did, sequenceId);

            Assert.Equal(resultSchema,secondResultSchema);
        }

        public async Task InitializeAsync()
        {
            await Wallet.DeleteWalletAsync(IssuerConfig, Credentials);
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
