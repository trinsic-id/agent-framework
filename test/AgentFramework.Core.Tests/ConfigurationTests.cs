using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.AspNetCore.Runtime;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Models.Wallets;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace AgentFramework.Core.Tests
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

            Assert.NotNull(container.Resolve<IEventAggregator>());
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
            Assert.NotNull(container.Resolve<HttpMessageHandler>());
        }

        [Fact]
        public void AddAgentframeworkWithExtendedServiceResolves()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework();
            services.AddExtendedConnectionService<MockExtendedConnectionService>();

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();
            
            var result = container.Resolve<IConnectionService>();

            Assert.True(result.GetType() == typeof(MockExtendedConnectionService));
        }

        [Fact]
        public void AddAgentframeworkWithCustomHandler()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddAgentFramework();
            services.AddMessageHandler<MockMessageHandler>();

            // Initialize Autofac
            var builder = new ContainerBuilder();

            builder.Populate(services);

            // Build the final container
            var container = builder.Build();

            var result = container.Resolve<IMessageHandler>();

            Assert.True(result.GetType() == typeof(MockMessageHandler));
        }

        [Fact]
        public async Task RunHostingService()
        {
            var slim = new SemaphoreSlim(0, 1);
            var provisioned = false;

            var provisioningMock = new Mock<IProvisioningService>();
            provisioningMock
                .Setup(x => x.ProvisionAgentAsync(It.IsAny<ProvisioningConfiguration>()))
                .Callback(() => { slim.Release(); provisioned = true; })
                .Returns(Task.CompletedTask);

            var hostBuilder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAgentFramework();
                    services.AddSingleton(provisioningMock.Object);
                })
                .Build();

            // Start the host
            await hostBuilder.StartAsync();

            // Wait for semaphore
            await slim.WaitAsync();

            // Assert
            Assert.True(provisioned);
        }
    }
}
