using System;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Agent Configuration Builder
    /// </summary>
    public class AgentBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        internal AgentBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Services collection 
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
