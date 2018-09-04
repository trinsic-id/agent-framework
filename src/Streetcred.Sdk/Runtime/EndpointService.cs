using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Sovrin.Agents.Model;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;

namespace Streetcred.Sdk.Runtime
{
    public class EndpointService : IEndpointService
    {
        private readonly IWalletRecordService _recordService;

        public EndpointService(IWalletRecordService walletRecord)
        {
            this._recordService = walletRecord;
        }

        /// <inheritdoc />
        public async Task<AgentEndpoint> GetEndpointAsync(Wallet wallet)
        {
            var record = await _recordService.GetAsync<EndpointRecord>(wallet, EndpointRecord.RecordId);
            return record.Endpoint;
        }

        /// <inheritdoc />
        public async Task StoreEndpointAsync(Wallet wallet, AgentEndpoint endpoint)
        {
            var record = await _recordService.GetAsync<EndpointRecord>(wallet, EndpointRecord.RecordId);
            if (record == null)
            {
                record = new EndpointRecord { Endpoint = endpoint };
                await _recordService.AddAsync(wallet, record);
            }
            else
            {
                record.Endpoint = endpoint;
                await _recordService.UpdateAsync(wallet, record);
            }
        }

        private class EndpointRecord : WalletRecord
        {
            internal const string RecordId = "SingleRecord";

            public override string GetId() => RecordId;

            public override string GetTypeName() => "EndpointRecord";

            public AgentEndpoint Endpoint { get; set; }
        }
    }
}
