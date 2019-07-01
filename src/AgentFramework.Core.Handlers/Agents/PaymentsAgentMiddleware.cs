using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Payments;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Records;

namespace AgentFramework.Payments.Middleware
{
    public class PaymentsAgentMiddleware : IAgentMiddleware
    {
        private readonly IPaymentService paymentService;
        private readonly IWalletRecordService recordService;

        public PaymentsAgentMiddleware(
            IPaymentService paymentService,
            IWalletRecordService recordService)
        {
            this.paymentService = paymentService;
            this.recordService = recordService;
        }

        public async Task OnMessageAsync(IAgentContext agentContext, MessageContext messageContext)
        {
            var message = messageContext.GetMessage<InternalAgentMessage>();
            var decorator = message.FindDecorator<PaymentRequestDecorator>("payment_request");
            if (decorator != null)
            {
                var record = new PaymentRecord
                {
                    Details = decorator.Details
                };
                await record.TriggerAsync(PaymentTrigger.RequestReceived);
                await recordService.AddAsync(agentContext.Wallet, record);

                if (messageContext.ContextRecord != null)
                {
                    messageContext.ContextRecord.SetTag("PaymentRecordId", record.Id);
                    await recordService.UpdateAsync(agentContext.Wallet, messageContext.ContextRecord);
                }
            }
        }
    }
}
