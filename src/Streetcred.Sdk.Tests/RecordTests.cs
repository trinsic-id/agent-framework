using System;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Runtime;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class RecordTests : IAsyncLifetime
    {
        private const string Config = "{\"id\":\"test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";

        private Wallet _wallet;

        private readonly IWalletRecordService _recordService;

        public RecordTests()
        {
            _recordService = new WalletRecordService();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(Config, Credentials);
            }
            finally
            {
                _wallet = await Wallet.OpenWalletAsync(Config, Credentials);
            }
        }

        [Fact]
        public async Task CanStoreAndRetrieveRecordWithTags()
        {
            var record = new ConnectionRecord { ConnectionId = "123" };
            record.Tags.Add("tag1", "tagValue1");

            await _recordService.AddAsync(_wallet, record);

            var retrieved = await _recordService.GetAsync<ConnectionRecord>(_wallet, "123");

            Assert.NotNull(retrieved);
            Assert.Equal(retrieved.GetId(), record.GetId());
            Assert.True(retrieved.Tags.ContainsKey("tag1"));
            Assert.Equal("tagValue1", retrieved.Tags["tag1"]);
        }

        [Fact]
        public async Task CanStoreAndRetrieveRecordWithTagsUsingSearch()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            var record = new ConnectionRecord { ConnectionId = Guid.NewGuid().ToString() };
            record.Tags.Add(tagName, tagValue);

            await _recordService.AddAsync(_wallet, record);

            var search =
                await _recordService.SearchAsync<ConnectionRecord>(_wallet,
                    new SearchRecordQuery() { { tagName, tagValue } }, null, 100);

            var retrieved = search.Single();

            Assert.NotNull(retrieved);
            Assert.Equal(retrieved.GetId(), record.GetId());
            Assert.True(retrieved.Tags.ContainsKey(tagName));
            Assert.Equal(tagValue, retrieved.Tags[tagName]);
        }

        [Fact]
        public async Task CanUpdateRecordWithTags()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            var id = Guid.NewGuid().ToString();

            var record = new ConnectionRecord { ConnectionId = id };
            record.Tags.Add(tagName, tagValue);

            await _recordService.AddAsync(_wallet, record);

            var retrieved = await _recordService.GetAsync<ConnectionRecord>(_wallet, id);

            retrieved.MyDid = "123";
            retrieved.Tags[tagName] = "value";

            await _recordService.UpdateAsync(_wallet, retrieved);

            var updated = await _recordService.GetAsync<ConnectionRecord>(_wallet, id);

            Assert.NotNull(updated);
            Assert.Equal(updated.GetId(), record.GetId());
            Assert.True(updated.Tags.ContainsKey(tagName));
            Assert.Equal("value", updated.Tags[tagName]);
            Assert.Equal("123", updated.MyDid);
        }

        [Fact]
        public async Task ReturnsNullForNonExistentRecord()
        {
            var record = await _recordService.GetAsync<ConnectionRecord>(_wallet, Guid.NewGuid().ToString());
            Assert.Null(record);
        }

        [Fact]
        public async Task ReturnsEmptyListForNonExistentRecord()
        {
            var record = await _recordService.SearchAsync<ConnectionRecord>(
                _wallet,
                new SearchRecordQuery { { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() } }, null, 100);
            Assert.False(record.Any());
        }

        [Fact]
        public void InitialConnectionRecordIsInvitedAndHasTag()
        {
            var record = new ConnectionRecord();

            Assert.True(record.State == ConnectionState.Invited);
            Assert.True(record.Tags["State"] == ConnectionState.Invited.ToString("G"));
        }

        [Fact]
        public void InitialCredentialRecordIsOfferedAndHasTag()
        {
            var record = new CredentialRecord();

            Assert.True(record.State == CredentialState.Offered);
            Assert.True(record.Tags["State"] == CredentialState.Offered.ToString("G"));
        }

        public async Task DisposeAsync()
        {
            await _wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(Config, Credentials);
        }
    }
}
