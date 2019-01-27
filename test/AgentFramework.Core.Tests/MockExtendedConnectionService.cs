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
        public Task<ConnectionRecord> GetAsync(Wallet wallet, string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<ConnectionRecord>> ListAsync(Wallet wallet, ISearchQuery query = null, int count = 100)
        {
            throw new System.NotImplementedException();
        }

        public Task<ConnectionInvitationMessage> CreateInvitationAsync(Wallet wallet, InviteConfiguration config = null)
        {
            throw new System.NotImplementedException();
        }

        public Task RevokeInvitationAsync(Wallet wallet, string invitationId)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> AcceptInvitationAsync(Wallet wallet, ConnectionInvitationMessage offer)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> ProcessRequestAsync(Wallet wallet, ConnectionRequestMessage request, ConnectionRecord connection)
        {
            throw new System.NotImplementedException();
        }

        public Task AcceptRequestAsync(Wallet wallet, string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public Task ProcessResponseAsync(Wallet wallet, ConnectionResponseMessage response, ConnectionRecord connection)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> DeleteAsync(Wallet wallet, string connectionId)
        {
            throw new System.NotImplementedException();
        }
    }
}