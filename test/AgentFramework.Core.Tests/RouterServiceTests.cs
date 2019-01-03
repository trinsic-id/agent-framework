using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Helpers;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class RouterServiceTests : IAsyncLifetime
    {
        private string Config = "{\"id\":\"" + Guid.NewGuid() + "\"}";
        private const string WalletCredentials = "{\"key\":\"test_wallet_key\"}";

        private Wallet _wallet;

        private readonly IRouterService _routerService;

        private readonly ConcurrentBag<HttpRequestMessage> _messages = new ConcurrentBag<HttpRequestMessage>();

        public RouterServiceTests()
        {
            var walletService = new DefaultWalletRecordService(new DateTimeHelper());
            _routerService = new DefaultRouterService(walletService, new Mock<IMessagingService>().Object, new Mock<ILogger<DefaultRouterService>>().Object);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(Config, WalletCredentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            _wallet = await Wallet.OpenWalletAsync(Config, WalletCredentials);
        }

        public async Task DisposeAsync()
        {
            if (_wallet != null) await _wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(Config, WalletCredentials);
        }
        
        [Fact]
        public async Task GetRouteThrowsRecordNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _routerService.GetRouteRecordAsync(_wallet, "bad-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task DeleteRouteThrowsRecordNotFound()
        {
            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () => await _routerService.DeleteRouteRecordAsync(_wallet, "bad-id"));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }

        [Fact]
        public async Task CreateRouteThrowsArguementNullExceptions()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _routerService.CreateRouteRecordAsync(_wallet, null, "test"));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _routerService.CreateRouteRecordAsync(_wallet, "test", null));
        }
        
        [Fact]
        public async Task CanCreateAndDeleteRouteRecord()
        {
            string identifier = "dummy-identifier";
            string connectionId = "connection-id";

            await _routerService.CreateRouteRecordAsync(_wallet, identifier, connectionId);

            var record = await _routerService.GetRouteRecordAsync(_wallet, identifier);

            Assert.NotNull(record);

            await _routerService.DeleteRouteRecordAsync(_wallet, identifier);

            var ex = await Assert.ThrowsAsync<AgentFrameworkException>(async () =>
                await _routerService.GetRouteRecordAsync(_wallet, identifier));
            Assert.True(ex.ErrorCode == ErrorCode.RecordNotFound);
        }
    }
}
