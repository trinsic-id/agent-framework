using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model.Records;
using WebAgent.Models;

namespace WebAgent.Controllers
{
    public class ConnectionsController : Controller
    {
        private readonly IConnectionService connectionService;
        private readonly IWalletService walletService;
        private readonly WalletOptions walletOptions;

        public ConnectionsController(IConnectionService connectionService, IWalletService walletService, IOptions<WalletOptions> walletOptions)
        {
            this.connectionService = connectionService;
            this.walletService = walletService;
            this.walletOptions = walletOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var wallet = await walletService.GetWalletAsync(walletOptions.WalletConfiguration, walletOptions.WalletCredentials);
            return View(new ConnectionsViewModel
            {
                Connections = await connectionService.ListConnectedConnectionsAsync(wallet),
                Invitations = await connectionService.ListNegotiatingConnectionsAsync(wallet)
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvitation()
        {
            var wallet = await walletService.GetWalletAsync(walletOptions.WalletConfiguration, walletOptions.WalletCredentials);

            var invitation = await connectionService.CreateInvitationAsync(wallet);
            ViewData["Invitation"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invitation)));
            return View();
        }
    }
}
