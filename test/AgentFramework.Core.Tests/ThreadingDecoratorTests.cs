using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Runtime;
using Hyperledger.Indy.PoolApi;
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
        public void DoesntCreateThreadWhenNoInboundMessage()
        {
            var threadDecorator = new OutgoingThreadDecoratorHandler();

            var outgoingMessage = new OutgoingMessage
            {
                OutboundMessage = new MessagePayload(new ConnectionRequestMessage())
            };

            threadDecorator.ProcessAsync(outgoingMessage, _agent);

            Assert.Throws<AgentFrameworkException>(() => outgoingMessage.OutboundMessage.GetDecorator<ThreadDecorator>("thread"));
        }

        [Fact]
        public void CreatesNewThreadFromUnthreadedInboundMessage()
        {
            var threadDecorator = new OutgoingThreadDecoratorHandler();

            var outgoingMessage = new OutgoingMessage
            {
                InboundMessage = new MessagePayload(new ConnectionRequestMessage()),
                OutboundMessage = new MessagePayload(new ConnectionResponseMessage())
            };

            threadDecorator.ProcessAsync(outgoingMessage, _agent);

            var threadingBlock = outgoingMessage.OutboundMessage.GetDecorator<ThreadDecorator>("thread");
            
            Assert.True(threadingBlock.ThreadId == outgoingMessage.InboundMessage.GetMessageId());
            Assert.True(threadingBlock.SenderOrder == 0);
            Assert.True(threadingBlock.RecievedOrders.Count == 0);
        }

        [Fact]
        public void AddsToThreadFromThreadedInboundMessage()
        {
            var threadDecorator = new OutgoingThreadDecoratorHandler();

            var threadId = Guid.NewGuid().ToString();
            var inboundMessage = new MessagePayload(new ConnectionRequestMessage());
            inboundMessage.AddDecorator(new ThreadDecorator()
            {
                ThreadId = threadId
            }, "thread");

            var outgoingMessage = new OutgoingMessage
            {
                InboundMessage = inboundMessage,
                OutboundMessage = new MessagePayload(new ConnectionResponseMessage())
            };

            threadDecorator.ProcessAsync(outgoingMessage, _agent);

            var threadingBlock = outgoingMessage.OutboundMessage.GetDecorator<ThreadDecorator>("thread");

            Assert.True(threadingBlock.ThreadId == threadId);
            Assert.True(threadingBlock.SenderOrder == 0);
            Assert.True(threadingBlock.RecievedOrders.Count == 0);
        }
    }
}
