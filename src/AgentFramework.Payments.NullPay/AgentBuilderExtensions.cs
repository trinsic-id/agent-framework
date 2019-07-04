using AgentFramework.Core.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Payments.NullPay
{
    public static class AgentBuilderExtensions
    {
        public static AgentBuilder AddNullPay(this AgentBuilder agentBuilder)
        {
            agentBuilder.Services.AddHostedService<NullPayConfigurationService>();
            return agentBuilder;
        }
    }
}
