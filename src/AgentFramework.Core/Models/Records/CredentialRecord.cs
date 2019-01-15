using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stateless;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents a credential record in the agency wallet.
    /// </summary>
    /// <seealso cref="RecordBase" />
    public class CredentialRecord : RecordBase
    {
        private CredentialState _state;

        public CredentialRecord()
        {
            State = CredentialState.Offered;
        }

        public CredentialRecord ShallowCopy()
        {
            return (CredentialRecord)this.MemberwiseClone();
        }

        public CredentialRecord DeepCopy()
        {
            CredentialRecord copy = (CredentialRecord)this.MemberwiseClone();
            return copy;
        }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string TypeName => "AF.CredentialRecord";

        /// <summary>
        /// Gets or sets the definition identifier of this credential.
        /// </summary>
        /// <value>The credential definition identifier.</value>
        [JsonIgnore]
        public string CredentialDefinitionId
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the credential request json.
        /// </summary>
        /// <value>The request json.</value>
        public string RequestJson
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user values json.
        /// </summary>
        /// <value>The values json.</value>
        public string ValuesJson
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the credential offer json.
        /// </summary>
        /// <value>The offer json.</value>
        public string OfferJson
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the credential revocation identifier.
        /// This field is only present in the issuer wallet.
        /// </summary>
        /// <value>The credential revocation identifier.</value>
        [JsonIgnore]
        public string CredentialRevocationId
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the schema identifier.
        /// </summary>
        /// <value>The schema identifier.</value>
        [JsonIgnore]
        public string SchemaId
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the connection identifier associated with this credential.
        /// </summary>
        /// <value>The connection identifier.</value>
        [JsonIgnore]
        public string ConnectionId
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the credential request metadata json.
        /// This field is only present in the holder wallet.
        /// </summary>
        /// <value>The credential request metadata json.</value>
        public string CredentialRequestMetadataJson
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the credential identifier.
        /// This field is only present in the holder wallet.
        /// </summary>
        /// <value>The credential identifier.</value>
        [JsonIgnore]
        public string CredentialId
        {
            get => Get();
            set => Set(value);
        }

        #region State Machine Implementation
        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public CredentialState State
        {
            get => _state;
            set => Set(value, ref _state);
        }

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
            state.Configure(CredentialState.Offered).Permit(CredentialTrigger.Reject, CredentialState.Rejected);
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Issue, CredentialState.Issued);
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Reject, CredentialState.Rejected);
            state.Configure(CredentialState.Issued).Permit(CredentialTrigger.Revoke, CredentialState.Revoked);

            return state;
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of possible credential states
    /// </summary>
    public enum CredentialState
    {
        /// <summary>
        /// The offered
        /// </summary>
        Offered = 0,
        /// <summary>
        /// The requested
        /// </summary>
        Requested,
        /// <summary>
        /// The issued
        /// </summary>
        Issued,
        /// <summary>
        /// The rejected
        /// </summary>
        Rejected,
        /// <summary>
        /// The revoked
        /// </summary>
        Revoked
    }

    /// <summary>
    /// Enumeration of possible triggers that change the credentials state
    /// </summary>
    public enum CredentialTrigger
    {
        /// <summary>
        /// The request
        /// </summary>
        Request,
        /// <summary>
        /// The issue
        /// </summary>
        Issue,
        /// <summary>
        /// The reject
        /// </summary>
        Reject,
        /// <summary>
        /// The revoke
        /// </summary>
        Revoke
    }
}
