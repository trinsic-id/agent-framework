using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Helpers;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Runtime;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Xunit;

namespace AgentFramework.Core.Tests
{
    internal static class Scenarios
    {
        internal static async Task<(ConnectionRecord firstParty, ConnectionRecord secondParty)> EstablishConnectionAsync(
            IConnectionService connectionService,
            IProducerConsumerCollection<IAgentMessage> messages,
            Wallet firstWallet,
            Wallet secondWallet,
            bool autoConnectionFlow = false)
        {
            // Create invitation by the issuer
            var issuerConnectionId = Guid.NewGuid().ToString();

            var inviteConfig = new InviteConfiguration()
            {
                ConnectionId = issuerConnectionId,
                AutoAcceptConnection = autoConnectionFlow,
                MyAlias = new ConnectionAlias()
                {
                    Name = "Issuer",
                    ImageUrl = "www.issuerdomain.com/profilephoto"
                },
                TheirAlias = new ConnectionAlias()
                {
                    Name = "Holder",
                    ImageUrl = "www.holderdomain.com/profilephoto"
                }
            };

            // Issuer creates an invitation
            var invitation = await connectionService.CreateInvitationAsync(firstWallet, inviteConfig);

            var connectionIssuer = await connectionService.GetAsync(firstWallet, issuerConnectionId);

            Assert.Equal(ConnectionState.Invited, connectionIssuer.State);
            Assert.True(invitation.Name == inviteConfig.MyAlias.Name &&
                        invitation.ImageUrl == inviteConfig.MyAlias.ImageUrl);

            // Holder accepts invitation and sends a message request
            var holderConnectionId = await connectionService.AcceptInvitationAsync(secondWallet, invitation);
            var connectionHolder = await connectionService.GetAsync(secondWallet, holderConnectionId);

            Assert.Equal(ConnectionState.Negotiating, connectionHolder.State);

            // Issuer processes incoming message
            var issuerMessage = messages.OfType<ConnectionRequestMessage>().FirstOrDefault();
            Assert.NotNull(issuerMessage);

            // Issuer processes the connection request by storing it and accepting it if auto connection flow is enabled
            await connectionService.ProcessRequestAsync(firstWallet, issuerMessage, connectionIssuer);

            if (!autoConnectionFlow)
            {
                connectionIssuer = await connectionService.GetAsync(firstWallet, issuerConnectionId);
                Assert.Equal(ConnectionState.Negotiating, connectionIssuer.State);

                // Issuer accepts the connection request
                await connectionService.AcceptRequestAsync(firstWallet, issuerConnectionId);
            }

            connectionIssuer = await connectionService.GetAsync(firstWallet, issuerConnectionId);
            Assert.Equal(ConnectionState.Connected, connectionIssuer.State);

            // Holder processes incoming message
            var holderMessage = messages.OfType<ConnectionResponseMessage>().FirstOrDefault();
            Assert.NotNull(holderMessage);

            // Holder processes the response message by accepting it
            await connectionService.ProcessResponseAsync(secondWallet, holderMessage, connectionHolder);

            // Retrieve updated connection state for both issuer and holder
            connectionIssuer = await connectionService.GetAsync(firstWallet, issuerConnectionId);
            connectionHolder = await connectionService.GetAsync(secondWallet, holderConnectionId);

            Assert.True(connectionIssuer.Alias.Name == inviteConfig.TheirAlias.Name &&
                        connectionIssuer.Alias.ImageUrl == inviteConfig.TheirAlias.ImageUrl);
            Assert.True(connectionHolder.Alias.Name == inviteConfig.MyAlias.Name &&
                        connectionHolder.Alias.ImageUrl == inviteConfig.MyAlias.ImageUrl);

            return (connectionIssuer, connectionHolder);
        }

        internal static async Task<(ConnectionRecord firstParty, ConnectionRecord secondParty)> EstablishConnectionUsingAgencyAsync(
            IConnectionService connectionService,
            IRouterService routingService,
            IProducerConsumerCollection<IAgentMessage> messages,
            Wallet inviterWallet,
            Wallet invitee,
            Wallet agencyWallet)
        {
            // Inviter creates invitation
            var inviterConnectionId = Guid.NewGuid().ToString();

            var inviteConfig = new InviteConfiguration()
            {
                ConnectionId = inviterConnectionId,
                MyIdentity = new ConnectionIdentity(await Did.CreateAndStoreMyDidAsync(inviterWallet, "{}"), await Crypto.CreateKeyAsync(inviterWallet, "{}")),
                ServiceType = DidServiceTypes.Agency,
                MyAlias = new ConnectionAlias()
                {
                    Name = "Inviter",
                    ImageUrl = "www.issuerdomain.com/profilephoto"
                },
                TheirAlias = new ConnectionAlias()
                {
                    Name = "Invitee",
                    ImageUrl = "www.holderdomain.com/profilephoto"
                }
            };

            // Inviter creates routing records required to host the connection
            await CreateRoutingRecord(connectionService, routingService, messages, inviterWallet, agencyWallet,
                inviteConfig.MyIdentity.ConnectionKey);
            await CreateRoutingRecord(connectionService, routingService, messages, inviterWallet, agencyWallet,
                inviteConfig.MyIdentity.Did);

            // Inviter creates an invitation
            var invitation = await connectionService.CreateInvitationAsync(inviterWallet, inviteConfig);

            var connectionInviter = await connectionService.GetAsync(inviterWallet, inviterConnectionId);

            Assert.Equal(ConnectionState.Invited, connectionInviter.State);
            Assert.True(invitation.Name == inviteConfig.MyAlias.Name &&
                        invitation.ImageUrl == inviteConfig.MyAlias.ImageUrl);

            var acceptInviteConfig = new AcceptInviteConfiguration
            {
                ServiceType = DidServiceTypes.Agency,
                MyIdentity = new ConnectionIdentity(await Did.CreateAndStoreMyDidAsync(inviterWallet, "{}"))
            };

            //Invitee creates routing records required to host the connection
            await CreateRoutingRecord(connectionService, routingService, messages, invitee, agencyWallet,
                acceptInviteConfig.MyIdentity.Did);

            // Invitee accepts invitation and sends a message request
            var inviteeConnectionId = await connectionService.AcceptInvitationAsync(invitee, invitation, acceptInviteConfig);
            var connectionInvitee = await connectionService.GetAsync(invitee, inviteeConnectionId);

            Assert.Equal(ConnectionState.Negotiating, connectionInvitee.State);

            //Agency processes the incoming message
            var agencyMessage = messages.OfType<ForwardMessage>().FirstOrDefault();
            Assert.NotNull(agencyMessage);
            Assert.True(agencyMessage.To == inviteConfig.MyIdentity.ConnectionKey);

            //Agency processes the forward message
            await routingService.ProcessForwardMessageAsync(agencyWallet, agencyMessage);

            // Inviter processes incoming message
            var inviterMessage = messages.OfType<ConnectionRequestMessage>().FirstOrDefault();
            Assert.NotNull(inviterMessage);

            // Inviter processes the connection request by storing it and accepting it if auto connection flow is enabled
            await connectionService.ProcessRequestAsync(inviterWallet, inviterMessage, connectionInviter);

            connectionInviter = await connectionService.GetAsync(inviterWallet, inviterConnectionId);
            Assert.Equal(ConnectionState.Negotiating, connectionInviter.State);

            // Inviter accepts the connection request
            await connectionService.AcceptRequestAsync(inviterWallet, inviterConnectionId);

            connectionInviter = await connectionService.GetAsync(inviterWallet, inviterConnectionId);
            Assert.Equal(ConnectionState.Connected, connectionInviter.State);

            //Agency processes the incoming message
            agencyMessage = messages.OfType<ForwardMessage>().FirstOrDefault();
            Assert.NotNull(agencyMessage);
            Assert.True(agencyMessage.To == acceptInviteConfig.MyIdentity.Did);

            //Agency processes the forward message
            await routingService.ProcessForwardMessageAsync(agencyWallet, agencyMessage);

            // Invitee processes incoming message
            var inviteeMessage = messages.OfType<ConnectionResponseMessage>().FirstOrDefault();
            Assert.NotNull(inviteeMessage);

            // Invitee processes the response message by accepting it
            await connectionService.ProcessResponseAsync(invitee, inviteeMessage, connectionInvitee);

            // Retrieve updated connection state for both inviter and invitee
            connectionInviter = await connectionService.GetAsync(inviterWallet, inviterConnectionId);
            connectionInvitee = await connectionService.GetAsync(invitee, inviteeConnectionId);

            Assert.True(connectionInviter.Alias.Name == inviteConfig.TheirAlias.Name &&
                        connectionInviter.Alias.ImageUrl == inviteConfig.TheirAlias.ImageUrl);
            Assert.True(connectionInvitee.Alias.Name == inviteConfig.MyAlias.Name &&
                        connectionInvitee.Alias.ImageUrl == inviteConfig.MyAlias.ImageUrl);

            return (connectionInviter, connectionInvitee);
        }

        internal static async Task CreateRoutingRecord(
            IConnectionService connectionService,
            IRouterService routingService,
            IProducerConsumerCollection<IAgentMessage> messages,
            Wallet subjectWallet, 
            Wallet routerWallet,
            string recipientIdentifier)
        {
            //Establish a connection between the subject of a route and the routing agent
            (ConnectionRecord subjectConnection, ConnectionRecord routerConnection) = await EstablishConnectionAsync(connectionService, messages, subjectWallet, routerWallet, true);

            //Subject sents the router a create route message
            await routingService.SendCreateRouteMessage(subjectWallet, recipientIdentifier, subjectConnection);

            // Router processes incoming message
            var createRouteMessage = messages.OfType<CreateRouteMessage>().FirstOrDefault();
            Assert.NotNull(createRouteMessage);
            await routingService.ProcessCreateRouteMessageAsync(routerWallet, createRouteMessage, routerConnection);
            
            //TODO we will have acknowledgement messages in future
        }

        internal static async Task<(CredentialRecord issuerCredential, CredentialRecord holderCredential)> IssueCredentialAsync(
            ISchemaService schemaService, ICredentialService credentialService,
            IProducerConsumerCollection<IAgentMessage> messages,
            ConnectionRecord issuerConnection, ConnectionRecord holderConnection, Wallet issuerWallet, Wallet holderWallet,
            Pool pool, string proverMasterSecretId, bool revocable)
        {
            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(issuerWallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            (string definitionId, _) = await CreateDummySchemaAndNonRevokableCredDef(pool, issuerWallet, schemaService, issuer.Did, new[] { "first_name", "last_name" });
            
            var offerConfig = new OfferConfiguration()
            {
                ConnectionId = issuerConnection.Id,
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };
            
            // Send an offer to the holder using the established connection channel
            await credentialService.SendOfferAsync(issuerWallet, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await credentialService.ProcessOfferAsync(holderWallet, credentialOffer, holderConnection);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(holderWallet, proverMasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            await credentialService.AcceptOfferAsync(holderWallet, pool, holderCredentialId,
                new Dictionary<string, string>
                {
                    {"first_name", "Jane"},
                    {"last_name", "Doe"}
                });

            // Issuer retrieves credential request from cloud agent
            var credentialRequest = FindContentMessage<CredentialRequestMessage>(messages);
            Assert.NotNull(credentialRequest);

            // Issuer processes the credential request by storing it
            var issuerCredentialId =
                await credentialService.ProcessCredentialRequestAsync(issuerWallet, credentialRequest, issuerConnection);

            // Issuer accepts the credential requests and issues a credential
            await credentialService.IssueCredentialAsync(pool, issuerWallet, issuer.Did, issuerCredentialId);

            // Holder retrieves the credential from their cloud agent
            var credential = FindContentMessage<CredentialMessage>(messages);
            Assert.NotNull(credential);

            // Holder processes the credential by storing it in their wallet
            await credentialService.ProcessCredentialAsync(pool, holderWallet, credential, holderConnection);

            // Verify states of both credential records are set to 'Issued'
            var issuerCredential = await credentialService.GetAsync(issuerWallet, issuerCredentialId);
            var holderCredential = await credentialService.GetAsync(holderWallet, holderCredentialId);

            return (issuerCredential, holderCredential);
        }

        internal static async Task<(string,string)> CreateDummySchemaAndNonRevokableCredDef(Pool pool, Wallet wallet, ISchemaService schemaService, string issuerDid, string[] attributeValues)
        {
            // Creata a schema and credential definition for this issuer
            var schemaId = await schemaService.CreateSchemaAsync(pool, wallet, issuerDid,
                $"Test-Schema-{Guid.NewGuid().ToString()}", "1.0", attributeValues);
            return (await schemaService.CreateCredentialDefinitionAsync(pool, wallet, schemaId, issuerDid, false, 100, new Uri("http://mock/tails")), schemaId);
        }

        internal static async Task ProvisionAgent(ProvisioningConfiguration config)
        {
            var provisionService = new DefaultProvisioningService(new DefaultWalletRecordService(new DateTimeHelper()), new DefaultWalletService());
            await provisionService.ProvisionAgentAsync(config);
        }

        private static T FindContentMessage<T>(IEnumerable<IAgentMessage> collection)
            where T : IAgentMessage
            => collection.OfType<T>().Single();
    }
}