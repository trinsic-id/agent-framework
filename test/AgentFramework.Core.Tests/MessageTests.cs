using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Xunit;

namespace AgentFramework.Core.Tests
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