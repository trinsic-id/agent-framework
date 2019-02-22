using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Records;
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
    public class CredentialsController : Controller
    {
        private readonly IConnectionService _connectionService;
        private readonly IProvisioningService _provisioningService;
        private readonly ISchemaService _schemaService;
        private readonly ICredentialService _credentialService;
        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;
        private readonly WalletOptions _walletOptions;

        public CredentialsController(
            IConnectionService connectionService,
            IProvisioningService provisioningService,
            ISchemaService schemaService,
            ICredentialService credentialService, 
            IWalletService walletService, 
            IMessageService messageService,
            IPoolService poolService,
            IOptions<WalletOptions> walletOptions)
        {
            _connectionService = connectionService;
            _provisioningService = provisioningService;
            _schemaService = schemaService;
            _credentialService = credentialService;
            _walletService = walletService;
            _poolService = poolService;
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

            var credentialRecs = await _credentialService.ListAsync(context);

            List<CredentialViewModel> creds = new List<CredentialViewModel>();

            foreach (var credentialRecord in credentialRecs)
            {
                creds.Add(new CredentialViewModel
                {
                    Name = "Test Cred",
                    State = credentialRecord.State,
                    CreatedAt = credentialRecord.CreatedAtUtc ?? DateTime.UtcNow
                });
            }

            return View(new CredentialsViewModel
            {
                Credentials = creds
            });
        }
    
        [HttpPost]
        public async Task<IActionResult> OfferCredential(string connectionId)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials),
                Pool = await _poolService.GetPoolAsync("DefaultPool", 2)
            };

            var connection = await _connectionService.GetAsync(context, connectionId);

            var issuerDid = (await _provisioningService.GetProvisioningAsync(context.Wallet)).IssuerDid;
            var tailsUri = (await _provisioningService.GetProvisioningAsync(context.Wallet)).TailsBaseUri;

            var schemaId = await _schemaService.CreateSchemaAsync(context.Pool, context.Wallet,
                issuerDid, "Test Credential", "1.0",
                new[] {"Full Name"});

            var credentialDefinitionId = await _schemaService.CreateCredentialDefinitionAsync(context.Pool, context.Wallet, schemaId, issuerDid, Guid.NewGuid().ToString(),
                false, 100, new Uri(tailsUri));

            await _credentialService.SendOfferAsync(context, connectionId, new OfferConfiguration
            {
                CredentialAttributeValues = new Dictionary<string, string>()
                {
                    {"Full Name", connection.Alias.ToString()}
                },
                IssuerDid = issuerDid,
                CredentialDefinitionId = credentialDefinitionId
            });

            return RedirectToAction("Index");
        }

        public async Task AcceptOffer()
        {

        }

        public async Task IssueCredential()
        {

        }
    }
}
