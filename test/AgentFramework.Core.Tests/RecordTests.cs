using System;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Runtime;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class RecordTests : IAsyncLifetime
    {
        private const string Config = "{\"id\":\"test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";

        private Wallet _wallet;

        private readonly IWalletRecordService _recordService;

        public RecordTests()
        {
            _recordService = new DefaultWalletRecordService();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(Config, Credentials);
            }
            catch (WalletExistsException)
            {
            }
            finally
            {
                _wallet = await Wallet.OpenWalletAsync(Config, Credentials);
            }
        }

        [Fact]
        public async Task CanStoreAndRetrieveRecordWithTags()
        {
            var record = new ConnectionRecord { Id = "123" };
            record.SetTag("tag1", "tagValue1");

            await _recordService.AddAsync(_wallet, record);

            var retrieved = await _recordService.GetAsync<ConnectionRecord>(_wallet, "123");

            Assert.NotNull(retrieved);
            Assert.Equal(retrieved.Id, record.Id);
            Assert.NotNull(retrieved.GetTag("tag1"));
            Assert.Equal("tagValue1", retrieved.GetTag("tag1"));
        }

        [Fact]
        public async Task CanStoreAndRetrieveRecordWithTagsUsingSearch()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            var record = new ConnectionRecord { Id = Guid.NewGuid().ToString() };
            record.SetTag(tagName, tagValue);

            await _recordService.AddAsync(_wallet, record);

            var search =
                await _recordService.SearchAsync<ConnectionRecord>(_wallet,
                    SearchQuery.Equal(tagName, tagValue), null, 100);

            var retrieved = search.Single();

            Assert.NotNull(retrieved);
            Assert.Equal(retrieved.Id, record.Id);
            Assert.NotNull(retrieved.GetTag(tagName));
            Assert.Equal(tagValue, retrieved.GetTag(tagName));
        }

        [Fact]
        public async Task CanUpdateRecordWithTags()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            var id = Guid.NewGuid().ToString();

            var record = new ConnectionRecord { Id = id };
            record.SetTag(tagName, tagValue);

            await _recordService.AddAsync(_wallet, record);

            var retrieved = await _recordService.GetAsync<ConnectionRecord>(_wallet, id);

            retrieved.MyDid = "123";
            retrieved.SetTag(tagName, "value");

            await _recordService.UpdateAsync(_wallet, retrieved);

            var updated = await _recordService.GetAsync<ConnectionRecord>(_wallet, id);

            Assert.NotNull(updated);
            Assert.Equal(updated.Id, record.Id);
            Assert.NotNull(updated.GetTag(tagName));
            Assert.Equal("value", updated.GetTag(tagName));
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
                SearchQuery.Equal(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()), null, 100);
            Assert.False(record.Any());
        }

        [Fact]
        public void InitialConnectionRecordIsInvitedAndHasTag()
        {
            var record = new ConnectionRecord();

            Assert.True(record.State == ConnectionState.Invited);
            Assert.True(record.GetTag(nameof(ConnectionRecord.State)) == ConnectionState.Invited.ToString("G"));
        }

        [Fact]
        public void InitialCredentialRecordIsOfferedAndHasTag()
        {
            var record = new CredentialRecord();

            Assert.True(record.State == CredentialState.Offered);
            Assert.True(record.GetTag(nameof(CredentialRecord.State)) == CredentialState.Offered.ToString("G"));
        }

        public async Task DisposeAsync()
        {
            await _wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(Config, Credentials);
        }
    }
}
