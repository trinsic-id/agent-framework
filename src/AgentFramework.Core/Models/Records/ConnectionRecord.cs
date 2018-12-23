using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Did;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stateless;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents a connection record in the agency wallet.
    /// </summary>
    /// <seealso cref="RecordBase" />
    public class ConnectionRecord : RecordBase
    {
        private ConnectionState _state;

        public ConnectionRecord()
        {
            Services = new List<IDidService>();
            State = ConnectionState.Invited;
        }

        public ConnectionRecord ShallowCopy()
        {
            return (ConnectionRecord)this.MemberwiseClone();
        }

        public ConnectionRecord DeepCopy()
        {
            ConnectionRecord copy = (ConnectionRecord)this.MemberwiseClone();
            copy.Alias = new ConnectionAlias(Alias);
            copy.Services = Services.ToList();
            return copy;
        }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string TypeName => "AF.ConnectionRecord";

        /// <summary>
        /// Gets or sets my did.
        /// </summary>
        /// <value>My did.</value>
        [JsonIgnore]
        public string MyDid
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets my verkey
        /// </summary>
        /// <value>My vk.</value>
        [JsonIgnore]
        public string MyVk
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets their did.
        /// </summary>
        /// <value>Their did.</value>
        [JsonIgnore]
        public string TheirDid
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets their verkey.
        /// </summary>
        /// <value>Their vk.</value>
        [JsonIgnore]
        public string TheirVk
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the alias associated to the connection.
        /// </summary>
        /// <value>The connection alias.</value>
        public ConnectionAlias Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the associated services to the connection.
        /// </summary>
        /// <value>The endpoint.</value>
        public IList<IDidService> Services
        {
            get;
            set;
        }

        #region State Machine Implementation

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public ConnectionState State
        {
            get => _state;
            set => Set(value, ref _state);
        }

        /// <summary>
        /// Triggers the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="trigger">Trigger.</param>
        public Task TriggerAsync(ConnectionTrigger trigger) => GetStateMachine().FireAsync(trigger);

        private StateMachine<ConnectionState, ConnectionTrigger> GetStateMachine()
        {
            var state = new StateMachine<ConnectionState, ConnectionTrigger>(() => State, x => State = x);
            state.Configure(ConnectionState.Invited).Permit(ConnectionTrigger.InvitationAccept, ConnectionState.Negotiating);
            state.Configure(ConnectionState.Invited).Permit(ConnectionTrigger.Request, ConnectionState.Negotiating);
            state.Configure(ConnectionState.Negotiating).Permit(ConnectionTrigger.Request, ConnectionState.Connected);
            state.Configure(ConnectionState.Negotiating).Permit(ConnectionTrigger.Response, ConnectionState.Connected);
            return state;
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of possible connection states
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConnectionState
    {
        Invited = 0,
        Negotiating,
        Connected
    }

    /// <summary>
    /// Enumeration of possible triggers that change the connections state
    /// </summary>
    public enum ConnectionTrigger
    {
        InvitationAccept,
        Request,
        Response
    }
}
