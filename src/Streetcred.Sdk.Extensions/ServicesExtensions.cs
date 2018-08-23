using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Runtime;

namespace Streetcred.Sdk.Extensions
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Adds services registration for all SDK services
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddAgent(this IServiceCollection services)
        {
            services.AddSingleton<IRouterService, RouterService>();
            services.AddSingleton<ITailsService, TailsService>();
            services.AddSingleton<ISchemaService, SchemaService>();
            services.AddSingleton<ILedgerService, LedgerService>();
            services.AddSingleton<IWalletService, WalletService>();
            services.AddSingleton<IPoolService, PoolService>();
            services.AddSingleton<IWalletRecordService, WalletRecordService>();
            services.AddSingleton<IConnectionService, ConnectionService>();
            services.AddSingleton<IMessageSerializer, MessageSerializer>();
            services.AddSingleton<IAgencyCredentialService, AgencyCredentialService>();
            services.AddSingleton<IHolderCredentialService, HolderCredentialService>();
            services.AddSingleton<IEndpointService, EndpointService>();
        }

        /// <summary>
        /// Allows default agent configuration
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="options">Options.</param>
        public static void UseAgent(this IApplicationBuilder app, Action<AgentBuilder> options)
        {
            var builder = new AgentBuilder(app);

            options(builder);

            builder.Initialize();
        }
    }
}
