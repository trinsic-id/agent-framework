using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework.Core.Agents
{
    public class DefaultAgent : AgentBase
    {
        public DefaultAgent(
            IServiceProvider serviceProvider,
            Default.ConnectionHandler connectionHandler,
            Default.CredentialHandler credentialHandler,
            Default.ProofHandler proofHandler)
            : base(serviceProvider)
        {
            Handlers = new IHandler[]
            {
                connectionHandler,
                credentialHandler,
                proofHandler
            };
        }

        public override IEnumerable<IHandler> Handlers { get; }
    }
}