using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Xunit;

namespace AgentFramework.Core.Tests
{
    internal static class Scenarios
    {
        internal static async Task<(ConnectionRecord firstParty, ConnectionRecord secondParty)> EstablishConnectionAsync(
            IConnectionService connectionService,
            IProducerConsumerCollection<IAgentMessage> _messages,
            IAgentContext firstContext,
            IAgentContext secondContext,
            CreateInvitationResult initialInvitationResult = null,
            string inviteeconnectionId = null)
        {
            // Create invitation by the issuer
            var connectionSecondId = Guid.NewGuid().ToString();

            var inviteConfig = new InviteConfiguration()
            {
                ConnectionId = connectionSecondId,
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
            var invitationResult = initialInvitationResult ?? await connectionService.CreateInvitationAsync(firstContext, inviteConfig);

            var connectionFirst = await connectionService.GetAsync(firstContext, inviteeconnectionId ?? inviteConfig.ConnectionId);
            Assert.Equal(ConnectionState.Invited, connectionFirst.State);
            firstContext.Connection = connectionFirst;

            if (initialInvitationResult == null)
            { 
                Assert.True(invitationResult.Invitation.Name == inviteConfig.MyAlias.Name &&
                            invitationResult.Invitation.ImageUrl == inviteConfig.MyAlias.ImageUrl);
            }

            // Holder accepts invitation and sends a message request
            var acceptInvitationResult = await connectionService.AcceptInvitationAsync(secondContext, invitationResult.Invitation);
            var connectionSecond = secondContext.Connection = acceptInvitationResult.Connection;

            _messages.TryAdd(acceptInvitationResult.Request);

            Assert.Equal(ConnectionState.Negotiating, connectionSecond.State);

            // Issuer processes incoming message
            var issuerMessage = _messages.OfType<ConnectionRequestMessage>().FirstOrDefault();
            Assert.NotNull(issuerMessage);

            // Issuer processes the connection request by storing it and accepting it if auto connection flow is enabled
            connectionSecondId = await connectionService.ProcessRequestAsync(firstContext, issuerMessage);
            
            firstContext.Connection = connectionFirst = await connectionService.GetAsync(firstContext, connectionSecondId);
            Assert.Equal(ConnectionState.Negotiating, connectionFirst.State);

            // Issuer accepts the connection request
            var response = await connectionService.AcceptRequestAsync(firstContext, connectionSecondId);
            _messages.TryAdd(response);

            firstContext.Connection = connectionFirst = await connectionService.GetAsync(firstContext, connectionSecondId);
            Assert.Equal(ConnectionState.Connected, connectionFirst.State);

            // Holder processes incoming message
            var holderMessage = _messages.OfType<ConnectionResponseMessage>().FirstOrDefault();
            Assert.NotNull(holderMessage);

            // Holder processes the response message by accepting it
            await connectionService.ProcessResponseAsync(secondContext, holderMessage);

            // Retrieve updated connection state for both issuer and holder
            connectionFirst = await connectionService.GetAsync(firstContext, connectionFirst.Id);
            connectionSecond = await connectionService.GetAsync(secondContext, connectionSecond.Id);
            
            return (connectionFirst, connectionSecond);
        }
        
        internal static async Task<(CredentialRecord issuerCredential, CredentialRecord holderCredential)> IssueCredentialAsync(
            ISchemaService schemaService, ICredentialService credentialService,
            IProducerConsumerCollection<IAgentMessage> messages,
            ConnectionRecord issuerConnection, ConnectionRecord holderConnection, 
            IAgentContext issuerContext, 
            IAgentContext holderContext,
            Pool pool, string proverMasterSecretId, bool revocable, OfferConfiguration offerConfiguration = null)
        {
            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(issuerContext.Wallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Creata a schema and credential definition for this issuer
            (string definitionId, _) = await CreateDummySchemaAndNonRevokableCredDef(issuerContext, schemaService, issuer.Did, new[] { "first_name", "last_name" });
            
            var offerConfig = offerConfiguration ?? new OfferConfiguration
            {
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };
            
            // Send an offer to the holder using the established connection channel
            await credentialService.SendOfferAsync(issuerContext, issuerConnection.Id, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await credentialService.ProcessOfferAsync(holderContext, credentialOffer, holderConnection);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(holderContext.Wallet, proverMasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            await credentialService.AcceptOfferAsync(holderContext, holderCredentialId,
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
                await credentialService.ProcessCredentialRequestAsync(issuerContext, credentialRequest, issuerConnection);

            // Issuer accepts the credential requests and issues a credential
            await credentialService.IssueCredentialAsync(issuerContext, issuer.Did, issuerCredentialId);

            // Holder retrieves the credential from their cloud agent
            var credential = FindContentMessage<CredentialMessage>(messages);
            Assert.NotNull(credential);

            // Holder processes the credential by storing it in their wallet
            await credentialService.ProcessCredentialAsync(holderContext, credential, holderConnection);

            // Verify states of both credential records are set to 'Issued'
            var issuerCredential = await credentialService.GetAsync(issuerContext, issuerCredentialId);
            var holderCredential = await credentialService.GetAsync(holderContext, holderCredentialId);

            return (issuerCredential, holderCredential);
        }

        internal static async Task<(string,string)> CreateDummySchemaAndNonRevokableCredDef(IAgentContext context, ISchemaService schemaService, string issuerDid, string[] attributeValues)
        {
            // Creata a schema and credential definition for this issuer
            var schemaId = await schemaService.CreateSchemaAsync(context.Pool, context.Wallet, issuerDid,
                $"Test-Schema-{Guid.NewGuid().ToString()}", "1.0", attributeValues);
            return (await schemaService.CreateCredentialDefinitionAsync(context.Pool, context.Wallet, schemaId,  issuerDid, "Tag", false, 100, new Uri("http://mock/tails")), schemaId);
        }

        private static T FindContentMessage<T>(IEnumerable<IAgentMessage> collection)
            where T : IAgentMessage
            => collection.OfType<T>().Single();
    }
}