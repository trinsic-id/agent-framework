using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
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
            services.AddSingleton<ICredentialService, CredentialService>();
            services.AddSingleton<IEndpointService, EndpointService>();
            services.AddOptions<WalletOptions>();
            services.AddOptions<PoolOptions>();
        }

        public static void AddIssuerAgency(this IServiceCollection services, Action<IssuerAgencyConfiguration> agentConfiguration)
        {
            AddAgent(services);

            var defaultConfiguration = new IssuerAgencyConfiguration();
            agentConfiguration?.Invoke(defaultConfiguration);

            services.Configure<WalletOptions>((obj) =>
            {
                obj.WalletConfiguration = defaultConfiguration.WalletOptions.WalletConfiguration;
                obj.WalletCredentials = defaultConfiguration.WalletOptions.WalletCredentials;
            });

            services.Configure<PoolOptions>((obj) =>
            {
                obj.PoolName = defaultConfiguration.PoolOptions.PoolName;
                obj.GenesisFilename = defaultConfiguration.PoolOptions.GenesisFilename;
            });
        }

        /// <summary>
        /// Allows default agent configuration
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="options">Options.</param>
        public static void UseIssuerAgency(this IApplicationBuilder app, string route, Action<IssuerAgencyBuilder> options = null)
        {
            var walletService = app.ApplicationServices.GetService<IWalletService>();
            var poolService = app.ApplicationServices.GetService<IPoolService>();
            var endpointService = app.ApplicationServices.GetService<IEndpointService>();

            var builder = new IssuerAgencyBuilder(walletService, poolService, endpointService);

            options?.Invoke(builder);

            var walletOptions = app.ApplicationServices.GetService<IOptions<WalletOptions>>();
            var poolOptions = app.ApplicationServices.GetService<IOptions<PoolOptions>>();

            builder.Initialize(walletOptions.Value, poolOptions.Value);

            app.Map(PathString.FromUriComponent(route), a =>
            {
                a.UseMiddleware<IssuerAgencyMiddleware>();
            });
        }
    }
}
