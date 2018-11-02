using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stateless;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Models.Records
{
    /// <summary>
    /// Represents a proof record in the agency wallet
    /// </summary>
    /// <seealso cref="WalletRecord" />
    public class ProofRecord : WalletRecord
    {
        private ProofState _state;

        public ProofRecord()
        {
            State = ProofState.Requested;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public override string GetId() => Id;

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "ProofRecord";

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the proof request json.
        /// </summary>
        /// <value>The proof request json.</value>
        public string RequestJson { get; set; }

        /// <summary>
        /// Gets or sets the proof json.
        /// </summary>
        /// <value>The proof json.</value>
        public string ProofJson { get; set; }

        /// <summary>
        /// Gets or sets the connection identifier associated with this proof request.
        /// </summary>
        /// <value>The connection identifier.</value>
        public string ConnectionId { get; set; }

        #region State Machine Implementation

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ProofState State
        {
            get => _state;
            set
            {
                _state = value;
                Tags[TagConstants.State] = value.ToString("G");
            }
        }
        
        /// <summary>
        /// Triggers the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="trigger">Trigger.</param>
        public Task TriggerAsync(ProofTrigger trigger) => GetStateMachine().FireAsync(trigger);

        private StateMachine<ProofState, ProofTrigger> GetStateMachine()
        {
            var state = new StateMachine<ProofState, ProofTrigger>(() => State, x => State = x);
            state.Configure(ProofState.Requested).Permit(ProofTrigger.Accept, ProofState.Accepted);
            state.Configure(ProofState.Requested).Permit(ProofTrigger.Reject, ProofState.Rejected);
            return state;
        }
        #endregion
    }

    /// <summary>
    /// Enumeration of possible proof states
    /// </summary>
    public enum ProofState
    {
        Requested = 0,
        Accepted = 1,
        Rejected = 2
    }

    /// <summary>
    /// Enumeration of possible triggers that change the proofs state
    /// </summary>
    public enum ProofTrigger
    {
        Request,
        Accept,
        Reject
    }
}
