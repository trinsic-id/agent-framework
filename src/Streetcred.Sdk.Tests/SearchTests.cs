using System;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;
using Streetcred.Sdk.Runtime;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class SearchTests : IAsyncLifetime
    {
        private const string Config = "{\"id\":\"search_test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";

        private Wallet _wallet;

        private readonly IWalletRecordService _recordService;

        public SearchTests()
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
        public async Task CanFilterSearchableProperties()
        {
            await _recordService.AddAsync(_wallet,
                new ConnectionRecord {ConnectionId = "1", State = ConnectionState.Invited});
            await _recordService.AddAsync(_wallet,
                new ConnectionRecord {ConnectionId = "2", State = ConnectionState.Connected});

            var searchResult = await _recordService.SearchAsync<ConnectionRecord>(_wallet,
                new SearchRecordQuery {{ TagConstants.State, ConnectionState.Invited.ToString("G")}}, null, 10);

            Assert.Single(searchResult);
            Assert.Equal("1", searchResult.Single().ConnectionId);
        }

        [Fact]
        public async Task CanSearchMulipleProperties()
        {
            var record1 = new ConnectionRecord {State = ConnectionState.Connected, ConnectionId = "1"};
            var record2 = new ConnectionRecord
            {
                State = ConnectionState.Connected,
                ConnectionId = "2",
                Tags = {["tagName"] = "tagValue"}
            };
            var record3 = new ConnectionRecord
            {
                State = ConnectionState.Invited,
                ConnectionId = "3",
                Tags = {["tagName"] = "tagValue"}
            };

            await _recordService.AddAsync(_wallet, record1);
            await _recordService.AddAsync(_wallet, record2);
            await _recordService.AddAsync(_wallet, record3);


            var searchResult = await _recordService.SearchAsync<ConnectionRecord>(_wallet,
                new SearchRecordQuery
                {
                    {"State", ConnectionState.Connected.ToString("G")},
                    {"tagName", "tagValue"}

                }, null, 10);

            Assert.Single(searchResult);
            Assert.Equal("2", searchResult.Single().ConnectionId);
        }

        [Fact]
        public async Task ReturnsEmptyIfNoRecordsMatchCriteria()
        {
            await _recordService.AddAsync(_wallet,
                new ConnectionRecord
                {
                    ConnectionId = Guid.NewGuid().ToString(),
                    State = ConnectionState.Invited,
                    Tags = {["tagName"] = "tagValue"}
                });
            await _recordService.AddAsync(_wallet,
                new ConnectionRecord {ConnectionId = Guid.NewGuid().ToString(), State = ConnectionState.Connected});

            var searchResult = await _recordService.SearchAsync<ConnectionRecord>(_wallet,
                new SearchRecordQuery
                {
                    {"State", ConnectionState.Connected.ToString("G")},
                    {"tagName", "tagValue"}
                }, null, 10);

            Assert.Empty(searchResult);
        }

        public async Task DisposeAsync()
        {
            await _wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(Config, Credentials);
        }
    }
}