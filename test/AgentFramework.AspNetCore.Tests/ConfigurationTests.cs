using System;
using System.Net.Http;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.AspNetCore.Runtime;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using Autofac;
using Autofac.Core.Registration;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgentFramework.AspNetCore.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void AddAgentframeworkInjectsRequiredServices()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework();

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();

            Assert.NotNull(container.Resolve<IConnectionService>());
            Assert.NotNull(container.Resolve<ICredentialService>());
            Assert.NotNull(container.Resolve<IProofService>());
            Assert.NotNull(container.Resolve<ILedgerService>());
            Assert.NotNull(container.Resolve<ISchemaService>());
            Assert.NotNull(container.Resolve<IWalletRecordService>());
            Assert.NotNull(container.Resolve<IProvisioningService>());
            Assert.NotNull(container.Resolve<IMessageService>());
            Assert.NotNull(container.Resolve<ITailsService>());
            Assert.NotNull(container.Resolve<IWalletService>());

            Assert.NotNull(container.Resolve<HttpClient>());
            
            //TODO learn how to resolve multiple

            Assert.NotNull(container.Resolve<DefaultConnectionHandler>());
            Assert.NotNull(container.Resolve<DefaultCredentialHandler>());
            Assert.NotNull(container.Resolve<DefaultProofHandler>());
        }

        [Fact]
        public void AddAgentframeworkWithExtendedServiceResolves()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework(_ => _.AddExtendedConnectionService<MockExtendedConnectionService>());

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();
            
            var result = container.Resolve<IConnectionService>();

            Assert.True(result.GetType() == typeof(MockExtendedConnectionService));
        }

        [Fact]
        public void AddAgentframeworkWithOverrideHandlers()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework(_ => _.OverrideDefaultMessageHandlers());

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();

            Assert.Throws<ComponentNotRegisteredException>(() => container.Resolve<IMessageHandler>());
            Assert.Throws<ComponentNotRegisteredException>(() => container.Resolve<DefaultConnectionHandler>());
            Assert.Throws<ComponentNotRegisteredException>(() => container.Resolve<DefaultCredentialHandler>());
            Assert.Throws<ComponentNotRegisteredException>(() => container.Resolve<DefaultProofHandler>());
        }

        [Fact]
        public void AddAgentframeworkWithCustomHandler()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework(_ => _.OverrideDefaultMessageHandlers()
                .AddMessageHandler<MockMessageHandler>());

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();

            var result = container.Resolve<IMessageHandler>();

            Assert.True(result.GetType() == typeof(MockMessageHandler));
        }

        [Fact]
        public void AddAgentframeworkWithMemoryCacheLedgerServiceResolves()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework(_ => _.AddMemoryCacheLedgerService(
                new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(new TimeSpan(0, 10, 0))));

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();

            var result = container.Resolve<ILedgerService>();

            Assert.True(result.GetType() == typeof(MemoryCacheLedgerService));
        }
    }
}
