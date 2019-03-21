using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.TestHarness
{
    public static class Scenarios
    {
        internal static async Task EstablishConnectionAsync(MockAgent firstParty, MockAgent secondParty)
        {
            var slim = new SemaphoreSlim(0, 1);

            var connectionService = firstParty.GetService<IConnectionService>();
            var messsageService = firstParty.GetService<IMessageService>();

            (var invitation, var inviterConnection) = await connectionService.CreateInvitationAsync(firstParty.Context,
                new InviteConfiguration { AutoAcceptConnection = true });

            (var request, var inviteeConnection) =
                await connectionService.CreateRequestAsync(secondParty.Context, invitation);
            await messsageService.SendToConnectionAsync(secondParty.Context.Wallet, request,
                inviteeConnection, invitation.RecipientKeys.First());

            // Wait for connection to be established or continue after 30 sec timeout
            await slim.WaitAsync(TimeSpan.FromSeconds(30));

            var connectionRecord1 = await connectionService.GetAsync(firstParty.Context, inviterConnection.Id);
            var connectionRecord2 = await connectionService.GetAsync(secondParty.Context, inviteeConnection.Id);

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
