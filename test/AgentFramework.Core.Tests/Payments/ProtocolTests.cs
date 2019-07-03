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
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Decorators.Payments;

namespace AgentFramework.Core.Tests.Payments
{
    public class ProtocolTests : TestSingleWallet
    {
        [Fact(DisplayName = "Create two InProc agents and establish connection")]
        public async Task CreateInProcAgentsAndConnect()
        {
            var agents = await InProcAgent.CreatePairedAsync(true);

            Assert.NotNull(agents.Agent1);
            Assert.NotNull(agents.Agent2);

            Assert.Equal(ConnectionState.Connected, agents.Connection1.State);
            Assert.Equal(ConnectionState.Connected, agents.Connection2.State);

            await agents.Agent1.DisposeAsync();
            await agents.Agent2.DisposeAsync();
        }

        [Fact(DisplayName = "Send a payment request as decorator to basic message")]
        public async Task SendPaymentRequest()
        {
            // Create two agent hosts and establish pairwise connection between them
            var agents = await InProcAgent.CreatePairedAsync(true);

            // Setup a basic use case for payments by using basic messages
            // Any AgentMessage can be used
            var basicMessage = new BasicMessage { Content = "This is payment request" };
            var basicRecord = new BasicMessageRecord { Text = basicMessage.Content };
            await agents.Agent1.Records.AddAsync(agents.Agent1.Context.Wallet, basicRecord);

            var paymentAddress = await agents.Agent1.Payments.GetDefaultPaymentAddressAsync(agents.Agent1.Context);

            // Attach the payment request to the agent message
            var paymentRecord = await agents.Agent1.Payments.AttachPaymentRequestAsync(agents.Agent1.Context, basicMessage, new PaymentDetails
            {
                Id = basicRecord.Id,
                Total = new PaymentItem
                {
                    Amount = new PaymentAmount
                    {
                        Currency = "sov",
                        Value = 10
                    },
                    Label = "Total"
                }
            }, paymentAddress);

            // PaymentRecord expectations
            Assert.NotNull(paymentRecord);
            Assert.Equal(10UL, paymentRecord.Amount);
            Assert.Equal(paymentAddress.Address, paymentRecord.Address);
            Assert.Equal(PaymentState.Requested, paymentRecord.State);

            var decorator = basicMessage.FindDecorator<PaymentRequestDecorator>("payment_request");

            Assert.NotNull(decorator);

            var response = await agents.Agent1.Messages.SendAsync(agents.Agent1.Context.Wallet, basicMessage, agents.Connection1);

            await agents.Agent1.DisposeAsync();
            await agents.Agent2.DisposeAsync();
        }
    }
}
