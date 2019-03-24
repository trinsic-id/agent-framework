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

namespace AgentFramework.TestHarness
{
    public static class Scenarios
    {
        public static async Task<(ConnectionRecord firstParty, ConnectionRecord secondParty)> EstablishConnectionAsync(
            IConnectionService connectionService,
            IProducerConsumerCollection<AgentMessage> _messages,
            IAgentContext firstContext,
            IAgentContext secondContext,
            ConnectionInvitationMessage inviteMessage = null,
            string inviteeconnectionId = null)
        {
            // Create invitation by the issuer
            var connectionSecondId = Guid.NewGuid().ToString();

            var inviteConfig = new InviteConfiguration
            {
                ConnectionId = connectionSecondId,
                MyAlias = new ConnectionAlias
                {
                    Name = "Issuer",
                    ImageUrl = "www.issuerdomain.com/profilephoto"
                },
                TheirAlias = new ConnectionAlias
                {
                    Name = "Holder",
                    ImageUrl = "www.holderdomain.com/profilephoto"
                }
            };

            ConnectionRecord inviterConnection = null;

            if (inviteMessage == null)
                (inviteMessage, inviterConnection) = await connectionService.CreateInvitationAsync(firstContext, inviteConfig);

            var connectionFirst = await connectionService.GetAsync(firstContext, inviteeconnectionId ?? inviteConfig.ConnectionId);
            Assert.Equal(ConnectionState.Invited, connectionFirst.State);
            firstContext.Connection = connectionFirst;

            // Holder accepts invitation and sends a message request
            (var request, var inviteeConnection) = await connectionService.CreateRequestAsync(secondContext, inviteMessage);
            var connectionSecond = secondContext.Connection = inviteeConnection;

            _messages.TryAdd(request);

            Assert.Equal(ConnectionState.Negotiating, connectionSecond.State);

            // Issuer processes incoming message
            var issuerMessage = _messages.OfType<ConnectionRequestMessage>().FirstOrDefault();
            Assert.NotNull(issuerMessage);

            // Issuer processes the connection request by storing it and accepting it if auto connection flow is enabled
            connectionSecondId = await connectionService.ProcessRequestAsync(firstContext, issuerMessage);
            
            firstContext.Connection = connectionFirst = await connectionService.GetAsync(firstContext, connectionSecondId);
            Assert.Equal(ConnectionState.Negotiating, connectionFirst.State);

            // Issuer accepts the connection request
            (var response, var _) = await connectionService.CreateResponseAsync(firstContext, connectionSecondId);
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

        public static async Task<(CredentialRecord issuerCredential, CredentialRecord holderCredential)> IssueCredentialAsync(
            ISchemaService schemaService, ICredentialService credentialService,
            IProducerConsumerCollection<AgentMessage> messages,
            ConnectionRecord issuerConnection, ConnectionRecord holderConnection, 
            IAgentContext issuerContext, 
            IAgentContext holderContext,
            Pool pool, string proverMasterSecretId, bool revocable, OfferConfiguration offerConfiguration = null)
        {
            // Create an issuer DID/VK. Can also be created during provisioning
            var issuer = await Did.CreateAndStoreMyDidAsync(issuerContext.Wallet,
                new { seed = "000000000000000000000000Steward1" }.ToJson());

            // Create a schema and credential definition for this issuer
            var (definitionId, _) = await CreateDummySchemaAndNonRevokableCredDef(issuerContext, schemaService,
                issuer.Did, new[] {"first_name", "last_name"});
            
            var offerConfig = offerConfiguration ?? new OfferConfiguration
            {
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };
            
            // Send an offer to the holder using the established connection channel
            var (offerMessage, _) = await credentialService.CreateOfferAsync(issuerContext, offerConfig, issuerConnection.Id);
            messages.TryAdd(offerMessage);

            // Holder retrieves message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await credentialService.ProcessOfferAsync(holderContext, credentialOffer, holderConnection);

            // Holder creates master secret. Will also be created during wallet agent provisioning
            await AnonCreds.ProverCreateMasterSecretAsync(holderContext.Wallet, proverMasterSecretId);

            // Holder accepts the credential offer and sends a credential request
            (var request, var _) = await credentialService.CreateCredentialRequestAsync(holderContext, holderCredentialId,
                new Dictionary<string, string>
                {
                    {"first_name", "Jane"},
                    {"last_name", "Doe"}
                });
            messages.TryAdd(request);

            // Issuer retrieves credential request from cloud agent
            var credentialRequest = FindContentMessage<CredentialRequestMessage>(messages);
            Assert.NotNull(credentialRequest);

            // Issuer processes the credential request by storing it
            var issuerCredentialId =
                await credentialService.ProcessCredentialRequestAsync(issuerContext, credentialRequest, issuerConnection);

            // Issuer accepts the credential requests and issues a credential
            (var credentialMessage, var _) = await credentialService.CreateCredentialAsync(issuerContext, issuer.Did, issuerCredentialId);
            messages.TryAdd(credentialMessage);

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

        public static async Task<(string,string)> CreateDummySchemaAndNonRevokableCredDef(IAgentContext context, ISchemaService schemaService, string issuerDid, string[] attributeValues)
        {
            // Create a schema and credential definition for this issuer
            var schemaId = await schemaService.CreateSchemaAsync(await context.Pool, context.Wallet, issuerDid,
                $"Test-Schema-{Guid.NewGuid().ToString()}", "1.0", attributeValues);
            return (await schemaService.CreateCredentialDefinitionAsync(await context.Pool, context.Wallet, schemaId,  issuerDid, "Tag", false, 100, new Uri("http://mock/tails")), schemaId);
        }

        private static T FindContentMessage<T>(IEnumerable<AgentMessage> collection)
            where T : AgentMessage
            => collection.OfType<T>().Single();
    }
}