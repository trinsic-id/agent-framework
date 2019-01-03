using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Routing;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Did;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Runtime
{
    /// <inheritdoc />
    public class DefaultRouterService : IRouterService
    {
        protected readonly IWalletRecordService WalletRecordService;
        protected readonly IMessagingService MessagingService;
        protected readonly ILogger<DefaultRouterService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AgentFramework.Core.Runtime.DefaultRouterService"/> class.
        /// </summary>
        public DefaultRouterService(IWalletRecordService walletRecordService, IMessagingService messagingService, ILogger<DefaultRouterService> logger)
        {
            WalletRecordService = walletRecordService;
            MessagingService = messagingService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task SendCreateRouteMessage(Wallet wallet, string recipientIdentifier, ConnectionRecord routerConnection)
        {
            var createRouteMessage = new CreateRouteMessage
            {
                RecipientIdentifier = recipientIdentifier
            };
            await MessagingService.SendAsync(wallet, createRouteMessage, routerConnection);
        }

        /// <inheritdoc />
        public virtual async Task SendDeleteMessageRoute(Wallet wallet, string recipientIdentifier,
            ConnectionRecord routerConnection)
        {
            var deleteRouteMessage = new DeleteRouteMessage
            {
                RecipientIdentifier = recipientIdentifier
            };
            await MessagingService.SendAsync(wallet, deleteRouteMessage, routerConnection);
        }

        /// <inheritdoc />
        public virtual async Task<RouteRecord> GetRouteRecordAsync(Wallet wallet, string id)
        {
            Logger.LogInformation(LoggingEvents.GetConnection, "Id {0}", id);

            var record = await WalletRecordService.GetAsync<RouteRecord>(wallet, id);

            if (record == null)
                throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Route record not found");

            return record;
        }

        /// <inheritdoc />
        public virtual async Task<IList<RouteRecord>> GetRoutesRecordsAsync(Wallet wallet, string connectionId = null)
        {
            ISearchQuery query = null;

            if (!string.IsNullOrEmpty(connectionId))
                query =  SearchQuery.Equal(nameof(RouteRecord.ConnectionId), connectionId);

            return await WalletRecordService.SearchAsync<RouteRecord>(wallet, query, null, 100);
        }

        /// <inheritdoc />
        public virtual async Task CreateRouteRecordAsync(Wallet wallet, string recipientIdentifier, string connectionId)
        {
            if (string.IsNullOrEmpty(recipientIdentifier))
                throw new ArgumentNullException(nameof(recipientIdentifier));

            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentNullException(nameof(connectionId));

            var route = new RouteRecord
            {
                Id = recipientIdentifier,
                ConnectionId = connectionId
            };

            await WalletRecordService.AddAsync(wallet, route);
        }

        /// <inheritdoc />
        public virtual async Task DeleteRouteRecordAsync(Wallet wallet, string recipientIdentifier)
        {
            var record = await GetRouteRecordAsync(wallet, recipientIdentifier);
            await WalletRecordService.DeleteAsync<RouteRecord>(wallet, record.Id);
        }

        /// <inheritdoc />
        public virtual async Task ProcessForwardMessageAsync(Wallet wallet, ForwardMessage message)
        {
            var route = await WalletRecordService.GetAsync<RouteRecord>(wallet, message.To);

            var connection = await WalletRecordService.GetAsync<ConnectionRecord>(wallet, route.ConnectionId);

            var messageRawContents = Convert.FromBase64String(message.Message);

            await MessagingService.SendAsync(wallet, messageRawContents, connection);
        }

        /// <inheritdoc />
        public virtual async Task ProcessCreateRouteMessageAsync(Wallet wallet, CreateRouteMessage message, ConnectionRecord connection)
        {
            await CreateRouteRecordAsync(wallet, message.RecipientIdentifier, connection.Id);
            
            //TODO should prob send back a confirmation message
        }

        /// <inheritdoc />
        public virtual async Task ProcessDeleteRouteMessageAsync(Wallet wallet, DeleteRouteMessage message, ConnectionRecord connection)
        {
            var route = await GetRouteRecordAsync(wallet, message.RecipientIdentifier);

            if (route.ConnectionId != connection.Id)
                throw new AgentFrameworkException(ErrorCode.InvalidOperation, $"Cannot delete routing record with id : {message.RecipientIdentifier} because the route isn't owned by connection {connection.Id}");

            await DeleteRouteRecordAsync(wallet, message.RecipientIdentifier);

            //TODO should prob send back a confirmation message
        }
    }
}
