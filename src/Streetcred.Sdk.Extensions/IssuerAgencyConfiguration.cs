using System;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model.Wallets;

namespace Streetcred.Sdk.Extensions
{
    public class IssuerAgencyConfiguration
    {
        public IssuerAgencyConfiguration()
        {
            WalletOptions = new WalletOptions();
            PoolOptions = new PoolOptions();
        }

        internal WalletOptions WalletOptions { get; private set; }
        internal PoolOptions PoolOptions { get; private set; }

        public IssuerAgencyConfiguration WithWalletOptions(WalletOptions walletOptions)
        {
            WalletOptions = walletOptions;

            return this;
        }

        public IssuerAgencyConfiguration WithPoolOptions(PoolOptions poolOptions)
        {
            PoolOptions = poolOptions;

            return this;
        }
    }
}
