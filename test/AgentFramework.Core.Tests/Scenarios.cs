using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Credentials;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
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
            IProducerConsumerCollection<IAgentMessage> _messages,
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
            var issuerMessage = _messages.OfType<ConnectionRequestMessage>().FirstOrDefault();
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
            var holderMessage = _messages.OfType<ConnectionResponseMessage>().FirstOrDefault();
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
            var schemaId = await schemaService.CreateSchemaAsync(pool, issuerWallet, issuer.Did,
                $"Test-Schema-{Guid.NewGuid().ToString()}", "1.0", new[] { "first_name", "last_name" });
            var definitionId =
                await schemaService.CreateCredentialDefinitionAsync(pool, issuerWallet, schemaId, issuer.Did, revocable, 100, new Uri("http://mock/tails"));

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

        private static T FindContentMessage<T>(IEnumerable<IAgentMessage> collection)
            where T : IAgentMessage
            => collection.OfType<T>().Single();
    }
}