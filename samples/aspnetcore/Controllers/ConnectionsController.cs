using System;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebAgent.Models;

namespace WebAgent.Controllers
{
    public class ConnectionsController : Controller
    {
        private readonly IConnectionService _connectionService;
        private readonly IWalletService _walletService;
        private readonly IProvisioningService _provisioningService;
        private readonly WalletOptions _walletOptions;

        public ConnectionsController(
            IConnectionService connectionService, 
            IWalletService walletService, 
            IProvisioningService provisioningService, 
            IOptions<WalletOptions> walletOptions)
        {
            _connectionService = connectionService;
            _walletService = walletService;
            _provisioningService = provisioningService;
            _walletOptions = walletOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return View(new ConnectionsViewModel
            {
                Connections = await _connectionService.ListAsync(wallet)
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvitation()
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

            var invitation = await _connectionService.CreateInvitationAsync(wallet, new InviteConfiguration { AutoAcceptConnection = true });
            ViewData["Invitation"] = EncodeInvitation(invitation);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvitation(AcceptConnectionViewModel model)
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            var _ = await _connectionService.AcceptInvitationAsync(wallet, DecodeInvitation(model.InvitationDetails));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ViewInvitation(AcceptConnectionViewModel model)
        {
            ViewData["InvitationDetails"] = model.InvitationDetails;

            return View(DecodeInvitation(model.InvitationDetails));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            return View(await _connectionService.GetAsync(wallet, id));
        }

        /// <summary>
        /// Encodes the invitation to a base64 string which can be presented to the user as QR code or a deep link Url
        /// </summary>
        /// <returns>The invitation.</returns>
        /// <param name="invitation">Invitation.</param>
        public string EncodeInvitation(ConnectionInvitationMessage invitation)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invitation)));
        }

        /// <summary>
        /// Decodes the invitation from base64 to strongly typed object
        /// </summary>
        /// <returns>The invitation.</returns>
        /// <param name="invitation">Invitation.</param>
        public ConnectionInvitationMessage DecodeInvitation(string invitation)
        {
            return JsonConvert.DeserializeObject<ConnectionInvitationMessage>(Encoding.UTF8.GetString(Convert.FromBase64String(invitation)));
        }
    }
}
