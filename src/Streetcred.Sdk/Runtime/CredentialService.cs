using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;
using Streetcred.Sdk.Model.Records.Search;

namespace Streetcred.Sdk.Runtime
{
    /// <summary>
    /// Credential service.
    /// </summary>
    public abstract class CredentialService : ICredentialService
    {
        protected readonly IRouterService RouterService;
        protected readonly ILedgerService LedgerService;
        protected readonly IConnectionService ConnectionService;
        protected readonly IWalletRecordService RecordService;
        protected readonly IMessageSerializer MessageSerializer;
        protected readonly ISchemaService SchemaService;
        protected readonly ITailsService TailsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Streetcred.Sdk.Runtime.CredentialService" /> class.
        /// </summary>
        /// <param name="routerService">Router service.</param>
        /// <param name="ledgerService">Ledger service.</param>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="recordService">The record service.</param>
        /// <param name="messageSerializer">The message serializer.</param>
        /// <param name="schemaService">The schema service.</param>
        /// <param name="tailsService">The tails service.</param>
        protected CredentialService(
            IRouterService routerService,
            ILedgerService ledgerService,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            IMessageSerializer messageSerializer,
            ISchemaService schemaService,
            ITailsService tailsService)
        {
            LedgerService = ledgerService;
            ConnectionService = connectionService;
            RecordService = recordService;
            MessageSerializer = messageSerializer;
            SchemaService = schemaService;
            TailsService = tailsService;
            RouterService = routerService;
        }

        /// <inheritdoc />
        public Task<CredentialRecord> GetAsync(Wallet wallet, string credentialId) =>
            RecordService.GetAsync<CredentialRecord>(wallet, credentialId);

        /// <inheritdoc />
        public Task<List<CredentialRecord>> ListAsync(Wallet wallet, SearchRecordQuery query = null) =>
            RecordService.SearchAsync<CredentialRecord>(wallet, query, null);
    }
}
