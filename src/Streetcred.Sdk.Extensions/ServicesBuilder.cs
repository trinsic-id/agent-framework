using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Runtime;

namespace Streetcred.Sdk.Extensions
{
    public class ServicesBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        private bool _connectionServiceOverriden;
        private bool _credentialServiceOverriden;
        private bool _ledgerServiceOverriden;
        private bool _messageSerializerOverriden;
        private bool _poolServiceOverriden;
        private bool _proofServiceOverriden;
        private bool _provisioningServiceOverriden;
        private bool _routerServiceOverriden;
        private bool _schemaServiceOverriden;
        private bool _tailServiceOverriden;
        private bool _walletRecordServiceOverriden;
        private bool _walletServiceOverriden;

        public ServicesBuilder()
        {
            _serviceCollection = new ServiceCollection();
        }

        internal void RegisterServices(ref IServiceCollection serviceCollection)
        {
            foreach (var service in _serviceCollection)
                serviceCollection.Add(service);

            if (!_connectionServiceOverriden)
                serviceCollection.AddSingleton<IDefaultConnectionService, DefaultConnectionService>();
            if (!_credentialServiceOverriden)
                serviceCollection.AddSingleton<IDefaultCredentialService, DefaultCredentialService>();
            if (!_ledgerServiceOverriden)
                serviceCollection.AddSingleton<IDefaultLedgerService, DefaultLedgerService>();
            if (!_messageSerializerOverriden)
                serviceCollection.AddSingleton<IDefaultMessageSerializer, DefaultMessageSerializer>();
            if (!_poolServiceOverriden)
                serviceCollection.AddSingleton<IDefaultPoolService, DefaultPoolService>();
            if (!_proofServiceOverriden)
                serviceCollection.AddSingleton<IDefaultProofService, DefaultProofService>();
            if (!_provisioningServiceOverriden)
                serviceCollection.AddSingleton<IDefaultProvisioningService, DefaultProvisioningService>();
            if (!_routerServiceOverriden)
                serviceCollection.AddSingleton<IDefaultRouterService, DefaultRouterService>();
            if (!_schemaServiceOverriden)
                serviceCollection.AddSingleton<IDefaultSchemaService, DefaultSchemaService>();
            if (!_tailServiceOverriden)
                serviceCollection.AddSingleton<IDefaultTailsService, DefaultTailsService>();
            if (!_walletRecordServiceOverriden)
                serviceCollection.AddSingleton<IDefaultWalletRecordService, DefaultWalletRecordService>();
            if (!_walletServiceOverriden)
                serviceCollection.AddSingleton<IDefaultWalletService, DefaultWalletService>();
        }
        
        public ServicesBuilder AddExtendedConnectionService<TService, TImplementation>()
            where TService : class, IDefaultConnectionService
            where TImplementation : class, TService, IDefaultConnectionService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultConnectionService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _connectionServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedConnectionService<TImplementation>()
            where TImplementation : class, IDefaultConnectionService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultConnectionService>(x => x.GetService<TImplementation>());
            _connectionServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedCredentialService<TService, TImplementation>()
            where TService : class, IDefaultCredentialService
            where TImplementation : class, TService, IDefaultCredentialService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultCredentialService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _credentialServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedCredentialService<TImplementation>()
            where TImplementation : class, IDefaultCredentialService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultCredentialService>(x => x.GetService<TImplementation>());
            _credentialServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedLedgerService<TService, TImplementation>()
            where TService : class, IDefaultLedgerService
            where TImplementation : class, TService, IDefaultLedgerService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultLedgerService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _ledgerServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedLedgerService<TImplementation>()
            where TImplementation : class, IDefaultLedgerService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultLedgerService>(x => x.GetService<TImplementation>());
            _ledgerServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedMessageSerializer<TService, TImplementation>()
            where TService : class, IDefaultMessageSerializer
            where TImplementation : class, TService, IDefaultMessageSerializer
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultMessageSerializer>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _messageSerializerOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedMessageSerializer<TImplementation>()
            where TImplementation : class, IDefaultMessageSerializer
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultMessageSerializer>(x => x.GetService<TImplementation>());
            _messageSerializerOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedPoolService<TService, TImplementation>()
            where TService : class, IDefaultPoolService
            where TImplementation : class, TService, IDefaultPoolService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultPoolService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _poolServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedPoolService<TImplementation>()
            where TImplementation : class, IDefaultPoolService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultPoolService>(x => x.GetService<TImplementation>());
            _poolServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedProofService<TService, TImplementation>()
            where TService : class, IDefaultProofService
            where TImplementation : class, TService, IDefaultProofService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultProofService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _proofServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedProofService<TImplementation>()
            where TImplementation : class, IDefaultProofService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultProofService>(x => x.GetService<TImplementation>());
            _proofServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedProvisioningService<TService, TImplementation>()
            where TService : class, IDefaultProvisioningService
            where TImplementation : class, TService, IDefaultProvisioningService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultProvisioningService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _provisioningServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedProvisioningService<TImplementation>()
            where TImplementation : class, IDefaultProvisioningService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultProvisioningService>(x => x.GetService<TImplementation>());
            _provisioningServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedRouterService<TService, TImplementation>()
            where TService : class, IDefaultRouterService
            where TImplementation : class, TService, IDefaultRouterService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultRouterService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _routerServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedRouterService<TImplementation>()
            where TImplementation : class, IDefaultRouterService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultRouterService>(x => x.GetService<TImplementation>());
            _routerServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedSchemaService<TService, TImplementation>()
            where TService : class, IDefaultSchemaService
            where TImplementation : class, TService, IDefaultSchemaService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultSchemaService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _schemaServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedSchemaService<TImplementation>()
            where TImplementation : class, IDefaultSchemaService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultSchemaService>(x => x.GetService<TImplementation>());
            _schemaServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedTailsService<TService, TImplementation>()
            where TService : class, IDefaultTailsService
            where TImplementation : class, TService, IDefaultTailsService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultTailsService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _tailServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedTailsService<TImplementation>()
            where TImplementation : class, IDefaultTailsService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultTailsService>(x => x.GetService<TImplementation>());
            _tailServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedWalletRecordService<TService, TImplementation>()
            where TService : class, IDefaultWalletRecordService
            where TImplementation : class, TService, IDefaultWalletRecordService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultWalletRecordService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _walletRecordServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedWalletRecordService<TImplementation>()
            where TImplementation : class, IDefaultWalletRecordService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultWalletRecordService>(x => x.GetService<TImplementation>());
            _walletRecordServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedWalletService<TService, TImplementation>()
            where TService : class, IDefaultWalletService
            where TImplementation : class, TService, IDefaultWalletService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultWalletService>(x => x.GetService<TImplementation>());
            _serviceCollection.AddSingleton<TService>(x => x.GetService<TImplementation>());
            _walletServiceOverriden = true;
            return this;
        }

        public ServicesBuilder AddExtendedWalletService<TImplementation>()
            where TImplementation : class, IDefaultWalletService
        {
            _serviceCollection.AddSingleton<TImplementation>();
            _serviceCollection.AddSingleton<IDefaultWalletService>(x => x.GetService<TImplementation>());
            _walletServiceOverriden = true;
            return this;
        }
    }
}
