using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Extensions.Options
{
    public class WalletOptions
    {
        public WalletOptions()
        {
            WalletConfiguration = new WalletConfiguration { Id = "DefaultWallet" };
            WalletCredentials = new WalletCredentials { Key = "DefaultKey" };
        }

        public WalletConfiguration WalletConfiguration
        {
            get;
            set;
        }

        public WalletCredentials WalletCredentials
        {
            get;
            set;
        }
    }
}
