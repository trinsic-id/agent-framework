using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.TestHarness
{
    public static class AgentScenarios
    {
        public static async Task<(ConnectionRecord inviteeConnection,ConnectionRecord inviterConnection)> EstablishConnectionAsync(MockAgent invitee, MockAgent inviter)
        {
            var slim = new SemaphoreSlim(0, 1);
            
            var connectionService = invitee.GetService<IConnectionService>();
            var messsageService = invitee.GetService<IMessageService>();

            // Hook into response message event of second runtime to release semaphore
            inviter.GetService<IEventAggregator>().GetEventByType<ServiceMessageProcessingEvent>()
                .Where(x => x.MessageType == MessageTypes.ConnectionResponse)
                .Subscribe(x => slim.Release());

            (var invitation, var inviterConnection) = await connectionService.CreateInvitationAsync(invitee.Context,
                new InviteConfiguration { AutoAcceptConnection = true });

            (var request, var inviteeConnection) =
                await connectionService.CreateRequestAsync(inviter.Context, invitation);
            await messsageService.SendToConnectionAsync(inviter.Context.Wallet, request,
                inviteeConnection, invitation.RecipientKeys.First());

            // Wait for connection to be established or continue after 30 sec timeout
            await slim.WaitAsync(TimeSpan.FromSeconds(30));

            var connectionRecord1 = await connectionService.GetAsync(invitee.Context, inviterConnection.Id);
            var connectionRecord2 = await connectionService.GetAsync(inviter.Context, inviteeConnection.Id);

            Assert.Equal(ConnectionState.Connected, connectionRecord1.State);
            Assert.Equal(ConnectionState.Connected, connectionRecord2.State);
            Assert.Equal(connectionRecord1.MyDid, connectionRecord2.TheirDid);
            Assert.Equal(connectionRecord1.TheirDid, connectionRecord2.MyDid);

            Assert.Equal(
                connectionRecord1.GetTag(TagConstants.LastThreadId),
                connectionRecord2.GetTag(TagConstants.LastThreadId));

            return (connectionRecord1, connectionRecord2);
        }

        public static async Task IssueCredential(MockAgent issuer, MockAgent holder, ConnectionRecord issuerConnection, ConnectionRecord holderConnection)
        {
            var credentialService = issuer.GetService<ICredentialService>();
            var messsageService = issuer.GetService<IMessageService>();
            var schemaService = issuer.GetService<ISchemaService>();
            var provisionService = issuer.GetService<IProvisioningService>();

            // Hook into message event
            var offerSlim = new SemaphoreSlim(0, 1);
            holder.GetService<IEventAggregator>().GetEventByType<ServiceMessageProcessingEvent>()
                .Where(x => x.MessageType == MessageTypes.CredentialOffer)
                .Subscribe(x => offerSlim.Release());

            var issuerProv = await provisionService.GetProvisioningAsync(issuer.Context.Wallet);

            var (definitionId, _) = await Scenarios.CreateDummySchemaAndNonRevokableCredDef(issuer.Context, schemaService,
                issuerProv.IssuerDid, new[] { "first_name", "last_name" });

            (var offer, var issuerCredentialRecord) = await credentialService.CreateOfferAsync(issuer.Context, new OfferConfiguration
            {
                IssuerDid = issuerProv.IssuerDid,
                CredentialDefinitionId = definitionId,
                CredentialAttributeValues = new Dictionary<string, string>()
                {
                    { "first_name", "Test" },
                    { "last_name", "Holder" }
                }
            }, issuerConnection.Id);
            await messsageService.SendToConnectionAsync(issuer.Context.Wallet, offer, issuerConnection);

            await offerSlim.WaitAsync(TimeSpan.FromSeconds(30));

            var offers = await credentialService.ListOffersAsync(holder.Context);
            
            Assert.NotNull(offers);
            Assert.True(offers.Count > 0);

            // Hook into message event
            var requestSlim = new SemaphoreSlim(0, 1);
            issuer.GetService<IEventAggregator>().GetEventByType<ServiceMessageProcessingEvent>()
                .Where(x => x.MessageType == MessageTypes.CredentialRequest)
                .Subscribe(x => requestSlim.Release());

            (var request, var holderCredentialRecord) = await credentialService.CreateCredentialRequestAsync(holder.Context, offers[0].Id);
            await messsageService.SendToConnectionAsync(holder.Context.Wallet, request, holderConnection);

            await requestSlim.WaitAsync(TimeSpan.FromSeconds(30));

            // Hook into message event
            var credentialSlim = new SemaphoreSlim(0, 1);
            holder.GetService<IEventAggregator>().GetEventByType<ServiceMessageProcessingEvent>()
                .Where(x => x.MessageType == MessageTypes.Credential)
                .Subscribe(x => credentialSlim.Release());

            (var cred, _) = await credentialService.CreateCredentialAsync(issuer.Context, issuerProv.IssuerDid,
                issuerCredentialRecord.Id);
            await messsageService.SendToConnectionAsync(issuer.Context.Wallet, cred, issuerConnection);

            await credentialSlim.WaitAsync(TimeSpan.FromSeconds(30));

            var issuerCredRecord = await credentialService.GetAsync(issuer.Context, issuerCredentialRecord.Id);
            var holderCredRecord = await credentialService.GetAsync(holder.Context, holderCredentialRecord.Id);

            Assert.Equal(CredentialState.Issued, issuerCredRecord.State);
            Assert.Equal(CredentialState.Issued, holderCredRecord.State);

            Assert.Equal(
                issuerCredRecord.GetTag(TagConstants.LastThreadId),
                holderCredRecord.GetTag(TagConstants.LastThreadId));
        }
    }
}
