using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stateless;

namespace Streetcred.Sdk.Model.Records
{
    /// <summary>
    /// Represents a credential record in the agency wallet
    /// </summary>
    /// <seealso cref="Streetcred.Services.Domain.Records.WalletRecord" />
    public class CredentialRecord : WalletRecord
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public override string GetId() => Id;

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "CredentialRecord";

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        #region State Machine Implementation

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public CredentialState State { get; set; }
        public string CredentialDefinitionId { get; set; }
        public string RequestJson { get; set; }
        public string ValuesJson { get; set; }
        public string OfferJson { get; set; }
        public string CredentialJson { get; set; }
        public string RevocId { get; set; }
        public string RevocRegDeltaJson { get; set; }
        public string ConnectionId { get; set; }
        public string CredentialRequestMetadataJson { get; set; }
        public string CredentialId { get; set; }

        /// <summary>
        /// Triggers the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="trigger">Trigger.</param>
        public Task TriggerAsync(CredentialTrigger trigger) => GetStateMachine().FireAsync(trigger);

        private StateMachine<CredentialState, CredentialTrigger> GetStateMachine()
        {
            var state = new StateMachine<CredentialState, CredentialTrigger>(() => State, x => State = x);
            state.Configure(CredentialState.Offered).Permit(CredentialTrigger.Request, CredentialState.Requested);
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Issue, CredentialState.Issued);
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Reject, CredentialState.Rejected);
            state.Configure(CredentialState.Issued).Permit(CredentialTrigger.Revoke, CredentialState.Revoked);

            return state;
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public enum CredentialState
    {
        Offered = 0,
        Requested,
        Issued,
        Rejected,
        Revoked
    }

    public enum CredentialTrigger
    {
        Request,
        Issue,
        Reject,
        Revoke
    }
}
