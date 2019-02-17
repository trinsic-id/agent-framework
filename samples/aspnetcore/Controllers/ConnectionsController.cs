using System;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Records.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebAgent.Messages;
using WebAgent.Models;
using WebAgent.Protocols;
using WebAgent.Protocols.BasicMessage;

namespace WebAgent.Controllers
{
    public class ConnectionsController : Controller
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IConnectionService _connectionService;
        private readonly IWalletService _walletService;
        private readonly IWalletRecordService _recordService;
        private readonly IProvisioningService _provisioningService;
        private readonly IMessageService _messageService;
        private readonly WalletOptions _walletOptions;

        public ConnectionsController(
            IEventAggregator eventAggregator,
            IConnectionService connectionService, 
            IWalletService walletService, 
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            IMessageService messageService,
            IOptions<WalletOptions> walletOptions)
        {
            _eventAggregator = eventAggregator;
            _connectionService = connectionService;
            _walletService = walletService;
            _recordService = recordService;
            _provisioningService = provisioningService;
            _messageService = messageService;
            _walletOptions = walletOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            return View(new ConnectionsViewModel
            {
                Connections = await _connectionService.ListAsync(context)
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvitation()
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            var invitation = await _connectionService.CreateInvitationAsync(context, new InviteConfiguration { AutoAcceptConnection = true });
            ViewData["Invitation"] = EncodeInvitation(invitation.Invitation);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvitation(AcceptConnectionViewModel model)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };
            var invite = DecodeInvitation(model.InvitationDetails);
            var invitationResult = await _connectionService.AcceptInvitationAsync(context, invite);
            await _messageService.SendAsync(context.Wallet, invitationResult.Request, invitationResult.Connection, invite.RecipientKeys[0]);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ViewInvitation(AcceptConnectionViewModel model)
        {
            ViewData["InvitationDetails"] = model.InvitationDetails;

            return View(DecodeInvitation(model.InvitationDetails));
        }

        [HttpPost]
        public async Task<IActionResult> SendTrustPing(string connectionId)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            var connection = await _connectionService.GetAsync(context, connectionId);

            var message = new TrustPingMessage
            {
                ResponseRequested = true,
                Comment = "Hello"
            };

            bool responseRecieved = false;

            _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
                .Where(_ => _.MessageType == CustomMessageTypes.TrustPingResponseMessageType)
                .Subscribe(_ =>
                {
                    responseRecieved = true;
                });

            await _messageService.SendAsync(context.Wallet, message, connection);


            var task = Task.Factory.StartNew(() =>
            {
                while (!responseRecieved) { }
                return true;
            });

            task.Wait(5000);

            if (responseRecieved)
                return RedirectToAction("Details", new { id = connectionId, trustPingSuccess = true });
            return RedirectToAction("Details", new { id = connectionId, trustPingSuccess = false });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id, bool? trustPingSuccess = null)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            var model = new ConnectionDetailsViewModel
            {
                Connection = await _connectionService.GetAsync(context, id),
                Messages = await _recordService.SearchAsync<BasicMessageRecord>(context.Wallet,
                    SearchQuery.Equal(nameof(BasicMessageRecord.ConnectionId), id), null, 10),
                TrustPingSuccess = trustPingSuccess
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string connectionId, string text)
        {
            if (string.IsNullOrEmpty(text)) return RedirectToAction("Details", new { id = connectionId });

            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            var messageRecord = new BasicMessageRecord
            {
                Id = Guid.NewGuid().ToString(),
                Direction = MessageDirection.Outgoing,
                Text = text,
                SentTime = DateTime.UtcNow,
                ConnectionId = connectionId
            };

            var message = new BasicMessage {Content = text};
            var connection = await _connectionService.GetAsync(context, connectionId);

            // Save the outgoing message to the local wallet for chat history purposes
            await _recordService.AddAsync(context.Wallet, messageRecord);

            // Send an agent message using the secure connection
            await _messageService.SendAsync(context.Wallet, message, connection);

            return RedirectToAction("Details", new {id = connectionId});
        }

        [HttpPost]
        public IActionResult LaunchApp(LaunchAppViewModel model)
        {
            return Redirect($"{model.UriSchema}{Uri.EscapeDataString(model.InvitationDetails)}");
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
