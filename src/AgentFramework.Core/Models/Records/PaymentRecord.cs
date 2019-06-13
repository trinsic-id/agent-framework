using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stateless;

namespace AgentFramework.Core.Models.Records
{
    public class PaymentRecord : RecordBase
    {
        private PaymentState _state;

        public override string TypeName => "AF.PaymentRecord";

        [JsonIgnore]
        public string RecordId
        {
            get => Get();
            set => Set(value);
        }

        [JsonIgnore]
        public string ReceiptId
        {
            get => Get();
            set => Set(value);
        }

        [JsonIgnore]
        public string Method
        {
            get => Get();
            set => Set(value);
        }

        [JsonIgnore]
        public string Address
        {
            get => Get();
            set => Set(value);
        }

        public string PaymentDetails { get; set; }

        public PaymentState State
        {
            get => _state;
            set => Set(value, ref _state);
        }

        public Task TriggerAsync(PaymentTrigger trigger) => GetStateMachine().FireAsync(trigger);

        private StateMachine<PaymentState, PaymentTrigger> GetStateMachine()
        {
            var state = new StateMachine<PaymentState, PaymentTrigger>(() => State, x => State = x);
            state.Configure(PaymentState.None).Permit(PaymentTrigger.RequestSent, PaymentState.Requested);
            state.Configure(PaymentState.None).Permit(PaymentTrigger.RequestReceived, PaymentState.RequestReceived);
            state.Configure(PaymentState.Requested).Permit(PaymentTrigger.ReceiptRecieved, PaymentState.ReceiptReceived);
            state.Configure(PaymentState.RequestReceived).Permit(PaymentTrigger.PaymentProcessed, PaymentState.Paid);
            return state;
        }
    }

    public enum PaymentState
    {
        None,
        Requested,
        RequestReceived,
        Paid,
        ReceiptReceived
    }

    public enum PaymentTrigger
    {
        RequestSent,
        RequestReceived,
        PaymentProcessed,
        ReceiptRecieved
    }
}
