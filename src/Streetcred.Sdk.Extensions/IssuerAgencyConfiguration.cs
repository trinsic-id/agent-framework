using Streetcred.Sdk.Extensions.Options;

namespace Streetcred.Sdk.Extensions
{
    public class IssuerAgencyConfiguration
    {
        internal IssuerAgencyConfiguration()
        {
            WalletOptions = new WalletOptions();
            PoolOptions = new PoolOptions();
        }

        internal WalletOptions WalletOptions { get; private set; }
        internal PoolOptions PoolOptions { get; private set; }

        /// <summary>
        /// Sets the <see cref="WalletOptions" /> for this agent
        /// </summary>
        /// <param name="walletOptions">The wallet options.</param>
        /// <returns></returns>
        public IssuerAgencyConfiguration WithWalletOptions(WalletOptions walletOptions)
        {
            WalletOptions = walletOptions;

            return this;
        }

        /// <summary>
        /// Sets the <see cref="PoolOptions"/> for this agent
        /// </summary>
        /// <param name="poolOptions">The pool options.</param>
        /// <returns></returns>
        public IssuerAgencyConfiguration WithPoolOptions(PoolOptions poolOptions)
        {
            PoolOptions = poolOptions;

            return this;
        }
    }
}
