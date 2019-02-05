using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Tests
{
    public class MockExtendedConnectionService : IConnectionService
    {
        public Task<ConnectionRecord> GetAsync(IAgentContext agentContext, string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<ConnectionRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100)
        {
            throw new System.NotImplementedException();
        }

        public Task<CreateInvitationResult> CreateInvitationAsync(IAgentContext agentContext, InviteConfiguration config = null)
        {
            throw new System.NotImplementedException();
        }

        public Task RevokeInvitationAsync(IAgentContext agentContext, string invitationId)
        {
            throw new System.NotImplementedException();
        }

        public Task<AcceptInvitationResult> AcceptInvitationAsync(IAgentContext agentContext, ConnectionInvitationMessage offer)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> ProcessRequestAsync(IAgentContext agentContext, ConnectionRequestMessage request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ConnectionResponseMessage> AcceptRequestAsync(IAgentContext agentContext, string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> DeleteAsync(IAgentContext agentContext, string connectionId)
        {
            throw new System.NotImplementedException();
        }

        Task<string> IConnectionService.ProcessResponseAsync(IAgentContext agentContext, ConnectionResponseMessage response)
        {
            throw new System.NotImplementedException();
        }
    }
}