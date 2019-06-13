using System;
using AgentFramework.Core.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Payments.SovrinToken
{
    public static class AgentBuilderExtensions
    {
        public static AgentBuilder AddSovrinToken(this AgentBuilder agentBuilder)
        {
            agentBuilder.Services.AddHostedService<SovrinTokenConfigurationService>();
            return agentBuilder;
        }
    }
}
