using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages.Common;
using AgentFramework.TestHarness;
using AgentFramework.TestHarness.Mock;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using AgentFramework.Core.Models.Connections;
using System.Linq;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Core.Tests.Payments
{
    public class ProtocolTests : TestSingleWallet
    {
        [Fact]
        public async Task SendPaymentRequest()
        {
            var agents = await InProcAgent.CreatePairedAsync(true);

            Assert.NotNull(agents.Agent1);
            Assert.NotNull(agents.Agent2);

            Assert.Equal(ConnectionState.Connected, agents.Connection1.State);
            Assert.Equal(ConnectionState.Connected, agents.Connection2.State);

            await agents.Agent1.DisposeAsync();
            await agents.Agent2.DisposeAsync();
        }
    }
}
