using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stateless;
using Streetcred.Sdk.Models.Connections;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Models.Records
{
    /// <summary>
    /// Represents a connection record in the agency wallet.
    /// </summary>
    /// <seealso cref="WalletRecord" />
    public class ConnectionRecord : WalletRecord
    {
        private ConnectionState _state;

        public ConnectionRecord()
        {
            State = ConnectionState.Invited;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public override string GetId() => ConnectionId;

        /// <summary>
        /// Gets or sets the connection identifier.
        /// </summary>
        /// <value>
        /// The connection identifier.
        /// </value>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string GetTypeName() => "ConnectionRecord";

        /// <summary>
        /// Gets or sets my did.
        /// </summary>
        /// <value>My did.</value>
        public string MyDid { get; set; }

        /// <summary>
        /// Gets or sets my verkey
        /// </summary>
        /// <value>My vk.</value>
        public string MyVk { get; set; }

        /// <summary>
        /// Gets or sets their did.
        /// </summary>
        /// <value>Their did.</value>
        public string TheirDid { get; set; }

        /// <summary>
        /// Gets or sets their verkey.
        /// </summary>
        /// <value>Their vk.</value>
        public string TheirVk { get; set; }

        /// <summary>
        /// Gets or sets the alias associated to the connection.
        /// </summary>
        /// <value>The connection alias.</value>
        public ConnectionAlias Alias { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>The endpoint.</value>
        public AgentEndpoint Endpoint { get; set; }

        #region State Machine Implementation

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ConnectionState State
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
