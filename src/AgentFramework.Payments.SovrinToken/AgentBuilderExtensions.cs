using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.Core.Handlers.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Payments.SovrinToken
{
    public static class AgentBuilderExtensions
    {
        public static AgentBuilder AddSovrinToken(this AgentBuilder agentBuilder)
        {
            agentBuilder.Services.AddHostedService<SovrinTokenConfigurationService>();
            agentBuilder.Services.AddSingleton<IPaymentService, SovrinPaymentService>();
            agentBuilder.Services.AddSingleton<IAgentMiddleware, PaymentsAgentMiddleware>();
            return agentBuilder;
        }
    }
}
