using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages.Connection;
using Streetcred.Sdk.Messages.Credentials;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Records;
using Xunit;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Tests
{
    internal static class Scenarios
    {
        internal static async Task<(ConnectionRecord firstParty, ConnectionRecord secondParty)> EstablishConnectionAsync(
            IConnectionService connectionService,
            IProducerConsumerCollection<IEnvelopeMessage> _messages,
            Wallet firstWallet,
            Wallet secondWallet,
            bool autoConnectionFlow = false)
        {
            // Create invitation by the issuer
            var issuerConnectionId = Guid.NewGuid().ToString();

            var inviteConfig = new DefaultCreateInviteConfiguration()
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
            var issuerMessage = _messages.OfType<ForwardToKeyEnvelopeMessage>()
                .First(x => x.Type.Contains(connectionIssuer.Tags.Single(item => item.Key == "connectionKey").Value));

            var requestMessage = GetContentMessage(issuerMessage) as ConnectionRequest;
            Assert.NotNull(requestMessage);

            // Issuer processes the connection request by storing it and accepting it if auto connection flow is enabled
            await connectionService.ProcessRequestAsync(firstWallet, requestMessage);

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
            var holderMessage = _messages.OfType<ForwardEnvelopeMessage>()
                .First(x => x.Type.Contains(connectionHolder.MyDid));

            var responseMessage = GetContentMessage(holderMessage) as ConnectionResponse;
            Assert.NotNull(responseMessage);

            // Holder processes the response message by accepting it
            await connectionService.ProcessResponseAsync(secondWallet, responseMessage);

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
            IProducerConsumerCollection<IEnvelopeMessage> messages,
            string issuerConnectionId, Wallet issuerWallet, Wallet holderWallet,
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

            var offerConfig = new DefaultCreateOfferConfiguration()
            {
                ConnectionId = issuerConnectionId,
                IssuerDid = issuer.Did,
                CredentialDefinitionId = definitionId
            };

            // Send an offer to the holder using the established connection channel
            await credentialService.SendOfferAsync(issuerWallet, offerConfig);

            // Holder retrives message from their cloud agent
            var credentialOffer = FindContentMessage<CredentialOfferMessage>(messages);

            // Holder processes the credential offer by storing it
            var holderCredentialId =
                await credentialService.ProcessOfferAsync(holderWallet, credentialOffer);

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
                await credentialService.ProcessCredentialRequestAsync(issuerWallet, credentialRequest);

            // Issuer accepts the credential requests and issues a credential
            await credentialService.IssueCredentialAsync(pool, issuerWallet, issuer.Did, issuerCredentialId);

            // Holder retrieves the credential from their cloud agent
            var credential = FindContentMessage<CredentialMessage>(messages);
            Assert.NotNull(credential);

            // Holder processes the credential by storing it in their wallet
            await credentialService.ProcessCredentialAsync(pool, holderWallet, credential);

            // Verify states of both credential records are set to 'Issued'
            var issuerCredential = await credentialService.GetAsync(issuerWallet, issuerCredentialId);
            var holderCredential = await credentialService.GetAsync(holderWallet, holderCredentialId);

            return (issuerCredential, holderCredential);
        }

        static IContentMessage GetContentMessage(IEnvelopeMessage message)
            => JsonConvert.DeserializeObject<IContentMessage>(message.Content);

        private static T FindContentMessage<T>(IEnumerable<IEnvelopeMessage> collection)
            where T : IContentMessage
            => collection.Select(GetContentMessage).OfType<T>().Single();
    }
}