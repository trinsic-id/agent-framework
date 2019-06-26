using System;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Payments;
using Newtonsoft.Json;
using Stateless;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents a payment record
    /// </summary>
    /// <seealso cref="AgentFramework.Core.Models.Records.RecordBase" />
    public sealed class PaymentRecord : RecordBase
    {
        private PaymentState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentRecord"/> class.
        /// </summary>
        public PaymentRecord()
        {
            Id = Guid.NewGuid().ToString();
            State = PaymentState.None;
        }

        public override string TypeName => "AF.PaymentRecord";

        /// <summary>
        /// Gets or sets the record associated with this payment.
        /// Ex: CredentialRecord, SchemaRecord, etc.
        /// </summary>
        [JsonIgnore]
        public string RecordId
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the receipt for this payment.
        /// Receipt can be represented as UTXO source.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the payment address or Payee Id.
        /// </summary>
        [JsonIgnore]
        public string Address
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the payment amount
        /// </summary>
        public ulong Amount { get; set; }

        /// <summary>
        /// Gets or sets the state of this record.
        /// </summary>
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
            state.Configure(PaymentState.None).Permit(PaymentTrigger.ProcessPayment, PaymentState.Paid);
            state.Configure(PaymentState.Requested).Permit(PaymentTrigger.ReceiptRecieved, PaymentState.ReceiptReceived);
            state.Configure(PaymentState.RequestReceived).Permit(PaymentTrigger.ProcessPayment, PaymentState.Paid);
            return state;
        }
    }

    public enum PaymentState
    {
        None = 0,
        Requested,
        RequestReceived,
        Paid,
        ReceiptReceived
    }

    public enum PaymentTrigger
    {
        RequestSent,
        RequestReceived,
        ProcessPayment,
        ReceiptRecieved
    }
}
