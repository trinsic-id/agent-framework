using System.Net.Http;
using AgentFramework.AspNetCore.Runtime;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Default;
using AgentFramework.Core.Runtime;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgentFramework.AspNetCore.Configuration.Service
{
    public static class ServiceBuilderExtensions
    {
        public static AgentServicesBuilder AddCoreServices(this AgentServicesBuilder builder)
        {
            builder.Services.TryAddSingleton<IConnectionService, DefaultConnectionService>();
            builder.Services.TryAddSingleton<ICredentialService, DefaultCredentialService>();
            builder.Services.TryAddSingleton<ILedgerService, DefaultLedgerService>();
            builder.Services.TryAddSingleton<IMessageSerializer, DefaultMessageSerializer>();
            builder.Services.TryAddSingleton<IPoolService, DefaultPoolService>();
            builder.Services.TryAddSingleton<IProofService, DefaultProofService>();
            builder.Services.TryAddSingleton<IProvisioningService, DefaultProvisioningService>();
            builder.Services.TryAddSingleton<IRouterService, DefaultRouterService>();
            builder.Services.TryAddSingleton<HttpClient>();
            builder.Services.TryAddSingleton<ISchemaService, DefaultSchemaService>();
            builder.Services.TryAddSingleton<ITailsService, DefaultTailsService>();
            builder.Services.TryAddSingleton<IWalletRecordService, DefaultWalletRecordService>();
            builder.Services.TryAddSingleton<IWalletService, DefaultWalletService>();

            builder.Services.TryAddSingleton<ConnectionHandler>();
            builder.Services.TryAddSingleton<CredentialHandler>();
            builder.Services.TryAddSingleton<ProofHandler>();

            return builder;
        }

        public static AgentServicesBuilder AddMemoryCacheLedgerService(this AgentServicesBuilder builder, MemoryCacheEntryOptions options = null)
        {
            builder.AddExtendedLedgerService<MemoryCacheLedgerService>()
                   .Services.AddMemoryCache()
                            .AddSingleton(_ => options);

            return builder;
        }

        public static AgentServicesBuilder AddExtendedConnectionService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IConnectionService
            where TImplementation : class, TService, IConnectionService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IConnectionService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedConnectionService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IConnectionService
        {
            builder.Services.AddSingleton<IConnectionService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedCredentialService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, ICredentialService
            where TImplementation : class, TService, ICredentialService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<ICredentialService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedCredentialService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, ICredentialService
        {
            builder.Services.AddSingleton<ICredentialService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedLedgerService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, ILedgerService
            where TImplementation : class, TService, ILedgerService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<ILedgerService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedLedgerService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, ILedgerService
        {
            builder.Services.AddSingleton<ILedgerService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedMessageSerializer<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IMessageSerializer
            where TImplementation : class, TService, IMessageSerializer
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IMessageSerializer>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedMessageSerializer<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IMessageSerializer
        {
            builder.Services.AddSingleton<IMessageSerializer, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedPoolService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IPoolService
            where TImplementation : class, TService, IPoolService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IPoolService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedPoolService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IPoolService
        {
            builder.Services.AddSingleton<IPoolService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedProofService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IProofService
            where TImplementation : class, TService, IProofService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IProofService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedProofService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IProofService
        {
            builder.Services.AddSingleton<IProofService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedProvisioningService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IProvisioningService
            where TImplementation : class, TService, IProvisioningService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IProvisioningService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedProvisioningService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IProvisioningService
        {
            builder.Services.AddSingleton<IProvisioningService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedRouterService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IRouterService
            where TImplementation : class, TService, IRouterService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IRouterService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedRouterService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IRouterService
        {
            builder.Services.AddSingleton<IRouterService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedSchemaService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, ISchemaService
            where TImplementation : class, TService, ISchemaService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<ISchemaService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedSchemaService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, ISchemaService
        {
            builder.Services.AddSingleton<ISchemaService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedTailsService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, ITailsService
            where TImplementation : class, TService, ITailsService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<ITailsService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedTailsService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, ITailsService
        {
            builder.Services.AddSingleton<ITailsService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedWalletRecordService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IWalletRecordService
            where TImplementation : class, TService, IWalletRecordService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IWalletRecordService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedWalletRecordService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IWalletRecordService
        {
            builder.Services.AddSingleton<IWalletRecordService, TImplementation>();
            return builder;
        }

        public static AgentServicesBuilder AddExtendedWalletService<TService, TImplementation>(this AgentServicesBuilder builder)
            where TService : class, IWalletService
            where TImplementation : class, TService, IWalletService
        {
            builder.Services.AddSingleton<TImplementation>();
            builder.Services.AddSingleton<IWalletService>(x => x.GetService<TImplementation>());
            builder.Services.AddSingleton<TService>(x => x.GetService<TImplementation>());
            return builder;
        }

        public static AgentServicesBuilder AddExtendedWalletService<TImplementation>(this AgentServicesBuilder builder)
            where TImplementation : class, IWalletService
        {
            builder.Services.AddSingleton<IWalletService, TImplementation>();
            return builder;
        }
    }
}
