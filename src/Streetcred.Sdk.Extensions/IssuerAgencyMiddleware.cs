using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sovrin.Agents.Model;
using Sovrin.Agents.Model.Connections;
using Sovrin.Agents.Model.Credentials;
using Sovrin.Agents.Model.Proofs;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;

namespace Streetcred.Sdk.Extensions
{
    public class IssuerAgencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;
        private readonly IMessageSerializer _messageSerializer;
        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;
        private readonly IProvisioningService _provisioningService;
        private readonly PoolOptions _poolOptions;
        private readonly WalletOptions _walletOptions;

        public IssuerAgencyMiddleware(RequestDelegate next,
                                      IWalletService walletService,
                                      IPoolService poolService,
                                      IMessageSerializer messageSerializer,
                                      IConnectionService connectionService,
                                      ICredentialService credentialService,
                                      IProvisioningService provisioningService,
                                      IOptions<WalletOptions> walletOptions,
                                      IOptions<PoolOptions> poolOptions)
        {
            _next = next;
            _walletService = walletService;
            _poolService = poolService;
            _messageSerializer = messageSerializer;
            _connectionService = connectionService;
            _credentialService = credentialService;
            _provisioningService = provisioningService;
            _poolOptions = poolOptions.Value;
            _walletOptions = walletOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var pool = await _poolService.GetPoolAsync(_poolOptions.PoolName);
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration, _walletOptions.WalletCredentials);
            var endpoint = await _provisioningService.GetProvisioningAsync(wallet);

            var body = new byte[(int)context.Request.ContentLength];

            await context.Request.Body.ReadAsync(body, 0, body.Length);

            var decrypted = await _messageSerializer.UnpackAsync<IEnvelopeMessage>(body, wallet, endpoint.Endpoint.Verkey);
            var decoded = JsonConvert.DeserializeObject<IContentMessage>(decrypted.Content);
            (var did, var _) = _messageSerializer.DecodeType(decoded.Type);

            switch (decoded)
            {
                case ConnectionRequest request:
                    await _connectionService.StoreRequestAsync(wallet, request);
                    break;
                case ConnectionResponse response:
                    await _connectionService.AcceptResponseAsync(wallet, response);
                    break;
                case CredentialOffer offer:
                    await _credentialService.StoreOfferAsync(wallet, offer, did);
                    break;
                case CredentialRequest request:
                    await _credentialService.StoreCredentialRequestAsync(wallet, request, did);
                    break;
                case Credential credential:
                    await _credentialService.StoreCredentialAsync(pool, wallet, credential, did);
                    break;
                case Proof _:
                    break;
            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
