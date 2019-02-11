using System;
using System.Net.Http.Headers;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using Xunit;

namespace AgentFramework.Core.Tests
{
    public class ThreadingDecoratorTests
    {
        [Fact]
        public void DoesntCreateThreadWhenNoInboundMessage()
        {
            var threadDecorator = new OutgoingThreadDecoratorHandler();

            var outgoingMessage = new OutgoingMessage
            {
                OutboundMessage = new MessagePayload(new ConnectionRequestMessage())
            };

            //TODO
            //threadDecorator.ProcessAsync()

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

            //TODO
            //threadDecorator.ProcessAsync()

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

            //TODO
            //threadDecorator.ProcessAsync()

            var threadingBlock = outgoingMessage.OutboundMessage.GetDecorator<ThreadDecorator>("thread");

            Assert.True(threadingBlock.ThreadId == threadId);
            Assert.True(threadingBlock.SenderOrder == 0);
            Assert.True(threadingBlock.RecievedOrders.Count == 0);
        }
    }
}
