using System;
using System.Threading.Tasks;
using AgentFramework.AspNetCore;
using AgentFramework.Core.Contracts;
using AgentFramework.Payments.SovrinToken;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models.Wallets;
using AgentFramework.Core.Handlers.Agents;
using System.IO;
using AgentFramework.Core.Models;
using Microsoft.Extensions.Options;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;

namespace AgentFramework.Core.Tests
{
    public abstract class TestSingleWallet : IAsyncLifetime
    {
        protected IAgentContext Context { get; private set; }
        public CreateAndStoreMyDidResult Trustee { get; private set; }
        public CreateAndStoreMyDidResult Trustee2 { get; private set; }
        public CreateAndStoreMyDidResult Trustee3 { get; private set; }
        protected IHost Host { get; private set; }

        public async Task DisposeAsync()
        {
            var walletOptions = Host.Services.GetService<IOptions<WalletOptions>>().Value;
            await Host.StopAsync();

            await Context.Wallet.CloseAsync();
            await Wallet.DeleteWalletAsync(walletOptions.WalletConfiguration.ToJson(), walletOptions.WalletCredentials.ToJson());
        }

        /// <summary>
        /// Create a single wallet and enable payments
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            Host = new HostBuilder()
                .ConfigureServices(services =>
                    services.AddAgentFramework(builder =>
                        builder
                            .AddIssuerAgent(config =>
                            {
                                config.EndpointUri = new Uri("http://test");
                                config.SupportPayments = true;
                                config.WalletConfiguration = new WalletConfiguration { Id = Guid.NewGuid().ToString() };
                                config.WalletCredentials = new WalletCredentials { Key = "test" };
                                config.GenesisFilename = Path.GetFullPath("pool_genesis.txn");
                                config.PoolName = "TestPool";
                            })
                            .AddSovrinToken()))
                .Build();

            await Host.StartAsync();
            await Pool.SetProtocolVersionAsync(2);

            Context = await Host.Services.GetService<IAgentProvider>().GetContextAsync();

            Trustee = await Did.CreateAndStoreMyDidAsync(Context.Wallet,
                new { seed = "000000000000000000000000Trustee1" }.ToJson());
            Trustee2 = await PromoteTrustee("000000000000000000000000Trustee2");
            Trustee3 = await PromoteTrustee("000000000000000000000000Trustee3");
        }

        async Task<CreateAndStoreMyDidResult> PromoteTrustee(string seed)
        {
            var trustee = await Did.CreateAndStoreMyDidAsync(Context.Wallet, new { seed = seed }.ToJson());

            await Ledger.SignAndSubmitRequestAsync(await Context.Pool, Context.Wallet, Trustee.Did,
                await Ledger.BuildNymRequestAsync(Trustee.Did, trustee.Did, trustee.VerKey, null, "TRUSTEE"));

            return trustee;
        }

        protected async Task<string> TrusteeMultiSignAndSubmitRequestAsync(string request)
        {
            var singedRequest1 = await Ledger.MultiSignRequestAsync(Context.Wallet, Trustee.Did, request);
            var singedRequest2 = await Ledger.MultiSignRequestAsync(Context.Wallet, Trustee2.Did, singedRequest1);
            var singedRequest3 = await Ledger.MultiSignRequestAsync(Context.Wallet, Trustee3.Did, singedRequest2);

            return await Ledger.SubmitRequestAsync(await Context.Pool, singedRequest3);
        }
    }
}
