using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class MessageTests : IAsyncLifetime
    {
        private Wallet _wallet;

        private const string Config = "{\"id\":\"message_test_wallet\"}";
        private const string Credentials = "{\"key\":\"test_wallet_key\"}";

        [Fact]
        public async Task CanParseDidPattern()
        {
            var did = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            var messageType = MessageUtils.FormatDidMessageType(did.Did, MessageTypes.ConnectionRequest);

            var (parsedDid, parsedType) = MessageUtils.ParseMessageType(messageType);

            Assert.Equal(did.Did, parsedDid);
            Assert.Equal(parsedType, MessageTypes.ConnectionRequest);
        }

        [Fact]
        public async Task CanParseKeyPattern()
        {
            var key = await Crypto.CreateKeyAsync(_wallet, "{}");

            var messageType = MessageUtils.FormatKeyMessageType(key, MessageTypes.ForwardToKey);

            var (parsedKey, parsedType) = MessageUtils.ParseMessageType(messageType);

            Assert.Equal(key, parsedKey);
            Assert.Equal(parsedType, MessageTypes.ForwardToKey);
        }

        [Fact]
        public void FindDidInsideRevocationRegistryId()
        {
            var revocationRegistryIdentifier =
                "Th7MpTaRZVRYnPiabds81Y:4:Th7MpTaRZVRYnPiabds81Y:3:CL:124:Tag:CL_ACCUM:Tag2";

            string did = null;
            var found = MessageUtils.FindFirstDid(revocationRegistryIdentifier, ref did);

            Assert.True(found);
            Assert.Equal("Th7MpTaRZVRYnPiabds81Y", did);
        }

        [Fact]
        public void FindAllDidsInsideRevocationRegistryId()
        {
            var revocationRegistryIdentifier =
                "4cU41vWW82ArfxJxHkzXPG:4:Th7MpTaRZVRYnPiabds81Y:3:CL:124:Tag:CL_ACCUM:Tag2";

            IEnumerable<string> dids = null;
            var found = MessageUtils.FindAllDids(revocationRegistryIdentifier, ref dids);

            Assert.True(found);
            var collection = dids.ToArray();
            Assert.Contains("Th7MpTaRZVRYnPiabds81Y", collection);
            Assert.Contains("4cU41vWW82ArfxJxHkzXPG", collection);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Wallet.CreateWalletAsync(Config, Credentials);
            }
            catch (WalletExistsException)
            {
            }
            finally
            {
                _wallet = await Wallet.OpenWalletAsync(Config, Credentials);
            }
        }

        public async Task DisposeAsync()
        {
            await _wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(Config, Credentials);
        }
    }
}