using System.Threading.Tasks;
using AgentFramework.TestHarness.Mock;
using Xunit;

namespace AgentFramework.Core.Tests.Payments
{
    public class ProtocolTests : TestSingleWallet
    {
        [Fact]
        public async Task SendPaymentRequest()
        {
            var (issuer, holder) = await InProcAgent.CreatePairedAsync();

            Assert.NotNull(issuer);
            Assert.NotNull(holder);

            await issuer.DisposeAsync();
            await holder.DisposeAsync();
        }
    }
}
