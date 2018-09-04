using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agency.Web.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Connections;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model.Records;

namespace Agency.Web.Controllers
{
    [Route("agent")]
    public class AgentController : Controller
    {
        private readonly IConnectionService _connectionService;
        private readonly IEndpointService _endpointService;
        private readonly IWalletService _walletService;
        private readonly WalletOptions _walletOptions;

        public AgentController(IConnectionService connectionService, IEndpointService endpointService,
            IWalletService walletService, IMessageSerializer messageSerializer, ICredentialService credentialService,
            IOptions<WalletOptions> walletOptions)
        {
            _connectionService = connectionService;
            _endpointService = endpointService;
            _walletService = walletService;
            _walletOptions = walletOptions.Value;
        }

        [HttpGet]
        public async Task<AgentEndpoint> Get()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return await _endpointService.GetEndpointAsync(wallet);
        }

        [HttpGet("invite")]
        public async Task<ConnectionInvitation> CreateInvitation()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return await _connectionService.CreateInvitationAsync(wallet, Guid.NewGuid().ToString());
        }

        [HttpGet("connections")]
        public async Task<List<ConnectionRecord>> ListConnections()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return await _connectionService.ListAsync(wallet);
        }
    }

}