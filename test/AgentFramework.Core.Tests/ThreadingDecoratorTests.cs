using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Messages.Connections;
using Hyperledger.Indy.WalletApi;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class ThreadingDecoratorTests : IAsyncLifetime
    {
        private readonly string _walletConfig = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";
        private IAgentContext _agent;

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(_walletConfig, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            try
            {
                await Wallet.CreateWalletAsync(_walletConfig, Credentials);
            }
            catch (WalletExistsException)
            {
                // OK
            }

            _agent = new AgentContext
            {
                Wallet = await Wallet.OpenWalletAsync(_walletConfig, Credentials),
            };
        }

        public async Task DisposeAsync()
        {
            if (_agent != null) await _agent.Wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(_walletConfig, Credentials);
        }

        [Fact]
        public void CreatesNewThreadFromUnthreadedInboundMessage()
        {
            var inboundMessage = new ConnectionRequestMessage();

            var outboundMessage = inboundMessage.CreateThreadedReply<ConnectionResponseMessage>();

            var threadingBlock = outboundMessage.GetDecorator<ThreadDecorator>("thread");

            Assert.True(threadingBlock.ThreadId == inboundMessage.Id);
            Assert.True(threadingBlock.SenderOrder == 0);
            Assert.True(threadingBlock.RecievedOrders.Count == 0);
        }

        [Fact]
        public void AddsToThreadFromThreadedInboundMessage()
        {
            var inboundMessage = new ConnectionRequestMessage();

            var threadId = Guid.NewGuid().ToString();
            inboundMessage.AddDecorator(new ThreadDecorator()
            {
                ThreadId = threadId
            }, "thread");

            var outgoingMessage = inboundMessage.CreateThreadedReply<ConnectionResponseMessage>();

            var threadingBlock = outgoingMessage.GetDecorator<ThreadDecorator>("thread");

            Assert.True(threadingBlock.ThreadId == threadId);
            Assert.True(threadingBlock.SenderOrder == 0);
            Assert.True(threadingBlock.RecievedOrders.Count == 0);
        }

        [Fact]
        public void ThreadFromThrowsExceptionOnAlreadyThreadedMessage()
        {
            var message = new ConnectionRequestMessage();

            var threadId = Guid.NewGuid().ToString();
            message.AddDecorator(new ThreadDecorator()
            {
                ThreadId = threadId
            }, "thread");

            var inboundMessage = new ConnectionInvitationMessage();

            var ex = Assert.Throws<AgentFrameworkException>(() => message.ThreadFrom(inboundMessage));
            Assert.True(ex.ErrorCode == ErrorCode.InvalidMessage);
        }
    }
}
