﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Extensions
{
    /// <summary>
    /// A collection of convenience methods for the <see cref="ICredentialService"/> class.
    /// </summary>
    public static class ConnectionServiceExtensions
    {
        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are in <see cref="ConnectionState.Negotiating"/> state.
        /// </summary>
        /// <returns>The negotiating connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListNegotiatingConnectionsAsync(
            this IConnectionService connectionService, IAgentContext agentContext, int count = 100)
            => connectionService.ListAsync(agentContext,
                SearchQuery.Equal(nameof(ConnectionRecord.State), ConnectionState.Negotiating.ToString("G")), count);

        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are in <see cref="ConnectionState.Connected"/> state.
        /// </summary>
        /// <returns>The connected connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListConnectedConnectionsAsync(
            this IConnectionService connectionService, IAgentContext agentContext, int count = 100)
            => connectionService.ListAsync(agentContext,
                SearchQuery.Equal(nameof(ConnectionRecord.State), ConnectionState.Connected.ToString("G")), count);

        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are in <see cref="ConnectionState.Invited"/> state.
        /// </summary>
        /// <returns>The invited connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListInvitedConnectionsAsync(
            this IConnectionService connectionService, IAgentContext agentContext, int count = 100)
            => connectionService.ListAsync(agentContext,
                SearchQuery.And(SearchQuery.Equal(nameof(ConnectionRecord.State), ConnectionState.Invited.ToString("G")),
                                SearchQuery.Equal(nameof(ConnectionRecord.MultiPartyInvitation), false.ToString())), count);

        /// <summary>
        /// Retrieves a list of <see cref="ConnectionRecord"/> that are multi-party invitations.
        /// </summary>
        /// <returns>The invited connections async.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="count">Count.</param>
        public static Task<List<ConnectionRecord>> ListMultiPartyInvitationsAsync(
            this IConnectionService connectionService, IAgentContext agentContext, int count = 100)
            => connectionService.ListAsync(agentContext,
                SearchQuery.And(SearchQuery.Equal(nameof(ConnectionRecord.State), ConnectionState.Invited.ToString("G")),
                    SearchQuery.Equal(nameof(ConnectionRecord.MultiPartyInvitation), true.ToString())), count);

        /// <summary>
        /// Retrieves a <see cref="ConnectionRecord"/> by key.
        /// </summary>
        /// <returns>The connection record.</returns>
        /// <param name="connectionService">Connection service.</param>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="myKey">My key.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="AgentFrameworkException">Throw with error code 'RecordNotFound' if a connection record for the key was not found</exception>
        public static async Task<ConnectionRecord> ResolveByMyKeyAsync(
            this IConnectionService connectionService, IAgentContext agentContext, string myKey)
        {
            if (string.IsNullOrEmpty(myKey))
                throw new ArgumentNullException(nameof(myKey));

            if (agentContext == null)
                throw new ArgumentNullException(nameof(agentContext));

            var record = (await connectionService.ListAsync(agentContext,
                             SearchQuery.Equal(nameof(ConnectionRecord.MyVk), myKey), 5)).SingleOrDefault()
                         ?? (await connectionService.ListAsync(agentContext,
                             SearchQuery.Equal(TagConstants.ConnectionKey, myKey), 5)).SingleOrDefault();
            
            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, $"Connection Record not found for key {myKey}");

            return record;
        }
    }
}