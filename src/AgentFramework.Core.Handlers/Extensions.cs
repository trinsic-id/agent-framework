using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Core.Handlers
{
    /// <summary>Extensions</summary>
    public static class Extensions
    {
        /// <summary>Wraps the message in payload.</summary>
        /// <param name="agentMessage">The agent message.</param>
        /// <returns></returns>
        public static MessagePayload AsMessagePayload(this AgentMessage agentMessage) =>
            new MessagePayload(agentMessage);

        /// <summary>Adds the default message handlers.</summary>
        /// <param name="collection">The collection.</param>
        public static void AddDefaultMessageHandlers(this IServiceCollection collection)
        {
            collection.AddTransient<DefaultConnectionHandler>();
            collection.AddTransient<DefaultCredentialHandler>();
            collection.AddTransient<DefaultProofHandler>();
            collection.AddTransient<DefaultForwardHandler>();
            collection.AddTransient<DefaultTrustPingMessageHandler>();
        }

        internal static AgentContext ToHandlerAgentContext(this IAgentContext context)
        {
            return new AgentContext
            {
                Wallet = context.Wallet,
                Pool = context.Pool,
                Connection = context.Connection,
                State = context.State
            };
        }
    }
}