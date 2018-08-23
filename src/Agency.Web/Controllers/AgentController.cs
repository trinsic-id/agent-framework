using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agency.Web.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Connections;
using Sovrin.Agents.Model.Credentials;
using Sovrin.Agents.Model.Proofs;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Model.Records;

namespace Agency.Web.Controllers
{
    [Route("agent")]
    public class AgentController : Controller
    {
        private readonly IConnectionService _connectionService;
        private readonly IEndpointService _endpointService;
        private readonly IWalletService _walletService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly IAgencyCredentialService _credentialService;

        public AgentController(IConnectionService connectionService, IEndpointService endpointService,
            IWalletService walletService, IMessageSerializer messageSerializer, IAgencyCredentialService credentialService)
        {
            _connectionService = connectionService;
            _endpointService = endpointService;
            _walletService = walletService;
            _messageSerializer = messageSerializer;
            _credentialService = credentialService;
        }

        [HttpGet]
        public async Task<AgentEndpoint> Get()
        {
            var wallet = await _walletService.GetWalletAsync(WalletUtils.Configuration, WalletUtils.Credentials);
            return await _endpointService.GetEndpointAsync(wallet);
        }

        [HttpGet("invite")]
        public async Task<ConnectionInvitation> CreateInvitation()
        {
            var wallet = await _walletService.GetWalletAsync(WalletUtils.Configuration, WalletUtils.Credentials);
            return await _connectionService.CreateInvitationAsync(wallet, Guid.NewGuid().ToString());
        }

        [HttpGet("connections")]
        public async Task<List<ConnectionRecord>> ListConnections()
        {
            var wallet = await _walletService.GetWalletAsync(WalletUtils.Configuration, WalletUtils.Credentials);
            return await _connectionService.ListAsync(wallet);
        }

        /// <summary>
        /// A2A endpoint supporting message types in the Sovrin ecosystem
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unsupported return result</exception>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] byte[] body)
        {
            var wallet = await _walletService.GetWalletAsync(WalletUtils.Configuration, WalletUtils.Credentials);
            var endpoint = await _endpointService.GetEndpointAsync(wallet);

            var decrypted =
                await _messageSerializer.UnpackAsync<IEnvelopeMessage>(body, wallet, endpoint.Verkey);

            var decoded = JsonConvert.DeserializeObject<IContentMessage>(decrypted.Content);
            var (did, _) = _messageSerializer.DecodeType(decoded.Type);

            switch (decoded)
            {
                case ConnectionRequest request:
                    await _connectionService.StoreRequestAsync(wallet, request);
                    break;
                case CredentialRequest request:
                    await _credentialService.StoreCredentialRequest(wallet, request, did);
                    break;
                case Proof _:
                    break;
            }

            throw new Exception("Unsupported return result");
        }
    }

}