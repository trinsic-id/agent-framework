using System.Threading.Tasks;
using AgentFramework.Core.Utils;
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
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Error, CredentialState.Offered);
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Issue, CredentialState.Issued);
            state.Configure(CredentialState.Requested).Permit(CredentialTrigger.Reject, CredentialState.Rejected);
            state.Configure(CredentialState.Issued).Permit(CredentialTrigger.Revoke, CredentialState.Revoked);
            state.Configure(CredentialState.Issued).Permit(CredentialTrigger.Error, CredentialState.Requested);

            return state;
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of possible credential states
    /// </summary>
    public enum CredentialState
    {
        Offered = 0,
        Requested,
        Issued,
        Rejected,
        Revoked
    }

    /// <summary>
    /// Enumeration of possible triggers that change the credentials state
    /// </summary>
    public enum CredentialTrigger
    {
        Request,
        Issue,
        Reject,
        Revoke,
        Error
    }
}
