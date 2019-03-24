using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.TestHarness
{
    public static class AgentScenarios
    {
        public static async Task EstablishConnectionAsync(MockAgent invitee, MockAgent inviter)
        {
            var slim = new SemaphoreSlim(0, 1);

            var connectionService = invitee.GetService<IConnectionService>();
            var connectionService2 = inviter.GetService<IConnectionService>();

            var messsageService2 = inviter.GetService<IMessageService>();

            // Hook into response message event of second runtime to release semaphore
            inviter.GetService<IEventAggregator>().GetEventByType<ServiceMessageProcessingEvent>()
                .Where(x => x.MessageType == MessageTypes.ConnectionResponse)
                .Subscribe(x => slim.Release());

            (var invitation, var inviterConnection) = await connectionService.CreateInvitationAsync(invitee.Context,
                new InviteConfiguration { AutoAcceptConnection = true });

            (var request, var inviteeConnection) =
                await connectionService2.CreateRequestAsync(inviter.Context, invitation);
            await messsageService2.SendToConnectionAsync(inviter.Context.Wallet, request,
                inviteeConnection, invitation.RecipientKeys.First());

            // Wait for connection to be established or continue after 30 sec timeout
            await slim.WaitAsync(TimeSpan.FromSeconds(30));

            var connectionRecord1 = await connectionService.GetAsync(invitee.Context, inviterConnection.Id);
            var connectionRecord2 = await connectionService2.GetAsync(inviter.Context, inviteeConnection.Id);

            Assert.Equal(ConnectionState.Connected, connectionRecord1.State);
            Assert.Equal(ConnectionState.Connected, connectionRecord2.State);
            Assert.Equal(connectionRecord1.MyDid, connectionRecord2.TheirDid);
            Assert.Equal(connectionRecord1.TheirDid, connectionRecord2.MyDid);

            Assert.Equal(
                connectionRecord1.GetTag(TagConstants.LastThreadId),
                connectionRecord2.GetTag(TagConstants.LastThreadId));
        }
    }
}
