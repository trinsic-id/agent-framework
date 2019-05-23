using System;
using AgentFramework.Core.Handlers;

namespace AgentFramework.AspNetCore
{
    /// <summary>
    /// Default agent.
    /// </summary>
    public class DefaultAgent : AgentBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.AspNetCore.DefaultAgent"/> class.
        /// </summary>
        /// <param name="provider">Provider.</param>
        public DefaultAgent(IServiceProvider provider) : base(provider)
        {
        }

        /// <summary>
        /// Configures the handlers.
        /// </summary>
        protected override void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddForwardHandler();
        }
    }
}
