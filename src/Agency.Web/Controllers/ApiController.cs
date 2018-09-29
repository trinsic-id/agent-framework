using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Records;

namespace Agency.Web.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly IConnectionService _connectionService;
        private readonly IProvisioningService _provisioningService;
        private readonly IWalletService _walletService;
        private readonly WalletOptions _walletOptions;

        public ApiController(IConnectionService connectionService, IProvisioningService provisioningService,
            IWalletService walletService, IMessageSerializer messageSerializer, ICredentialService credentialService,
            IOptions<WalletOptions> walletOptions)
        {
            _connectionService = connectionService;
            _provisioningService = provisioningService;
            _walletService = walletService;
            _walletOptions = walletOptions.Value;
        }

        [HttpGet]
        public async Task<ProvisioningRecord> Get()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return await _provisioningService.GetProvisioningAsync(wallet);
        }

        [HttpPost("invite")]
        public async Task<ConnectionInvitation> CreateInvitation([FromBody] CreateInviteConfiguration config = null)
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return await _connectionService.CreateInvitationAsync(wallet, config);
        }

        [HttpDelete("connections/{connectionId}")]
        public async Task<ActionResult> DeleteConnection(string connectionId)
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            
            if (await _connectionService.DeleteAsync(wallet, connectionId))
                return Ok();
            return BadRequest("Failed to delete the connection");
        }

        [HttpGet("connections")]
        public async Task<List<ConnectionRecord>> ListConnections()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return await _connectionService.ListAsync(wallet);
        }
    }

}