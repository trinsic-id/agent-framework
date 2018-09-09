using Streetcred.Sdk.Extensions.Options;

namespace Streetcred.Sdk.Extensions
{
    public class AgentConfiguration
    {
        internal AgentConfiguration()
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
        public AgentConfiguration SetWalletOptions(WalletOptions walletOptions)
        {
            WalletOptions = walletOptions;

            return this;
        }

        /// <summary>
        /// Sets the <see cref="PoolOptions"/> for this agent
        /// </summary>
        /// <param name="poolOptions">The pool options.</param>
        /// <returns></returns>
        public AgentConfiguration SetPoolOptions(PoolOptions poolOptions)
        {
            PoolOptions = poolOptions;

            return this;
        }
    }
}
