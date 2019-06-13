using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AgentFramework.Payments.SovrinToken
{
    internal class SovrinTokenConfigurationService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Configuration.InitializeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}