using System;
using System.Threading.Tasks;
using Streetcred.Sdk.Model.Records;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class StateMachineTests
    {
        [Fact]
        public async Task CanTransitionFromDisconnetedToNegotiatingWithInvitationCreate()
        {
            var record = new ConnectionRecord();

            Assert.True(ConnectionState.Disconnected == record.State);

            await record.TriggerAsync(ConnectionTrigger.InvitationCreate);

            Assert.True(ConnectionState.Negotiating == record.State);
        }

        [Fact]
        public async Task CanTransitionFromDisconnetedToNegotiatingWithInvitationAccept()
        {
            var record = new ConnectionRecord();

            Assert.True(ConnectionState.Disconnected == record.State);

            await record.TriggerAsync(ConnectionTrigger.InvitationAccept);

            Assert.True(ConnectionState.Negotiating == record.State);
        }

        [Fact]
        public async Task CanTransitionFromNegotiatingToConnectedWithRequest()
        {
            var record = new ConnectionRecord() { State = ConnectionState.Negotiating };

            Assert.True(ConnectionState.Negotiating == record.State);

            await record.TriggerAsync(ConnectionTrigger.Request);

            Assert.True(ConnectionState.Connected == record.State);
        }

        [Fact]
        public async Task CanTransitionFromNegotiatingToConnectedWithRespone()
        {
            var record = new ConnectionRecord() { State = ConnectionState.Negotiating };

            Assert.True(ConnectionState.Negotiating == record.State);

            await record.TriggerAsync(ConnectionTrigger.Response);

            Assert.True(ConnectionState.Connected == record.State);
        }

        [Fact]
        public async Task CannotTransitionFromDisconnectedToConnectedWithRequestOrResponse()
        {
            var record = new ConnectionRecord();

            Assert.True(ConnectionState.Disconnected == record.State);

            var exception =
               await Assert.ThrowsAsync<InvalidOperationException>(() => record.TriggerAsync(ConnectionTrigger.Request));

            await Assert.ThrowsAsync<InvalidOperationException>(() => record.TriggerAsync(ConnectionTrigger.Response));

            Assert.Equal("Stateless", exception.Source);
        }
    }
}
