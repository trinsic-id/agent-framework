using System;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers;
using AgentFramework.TestHarness.Utils;
using Hyperledger.Indy.DidApi;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models.Payments;
using AgentFramework.Core.Models.Records;
using Indy = Hyperledger.Indy.PaymentsApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using AgentFramework.Payments.SovrinToken;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Polly;

namespace AgentFramework.Core.Tests
{
    public class PaymentTests : TestSingleWallet
    {
        [Fact(DisplayName = "Create random payment address for Sovrin method")]
        public async Task CreateSovrinPaymentAddress()
        {
            var paymentService = Host.Services.GetService<IPaymentService>();
            var address = await paymentService.CreatePaymentAddressAsync(Context);

            Assert.NotNull(address);
            Assert.NotNull(address.Address);
        }

        [Fact(DisplayName = "Mint Sovrin tokens")]
        public async Task MintSovrinTokens()
        {
            var paymentService = Host.Services.GetService<IPaymentService>();
            var address = await paymentService.CreatePaymentAddressAsync(Context);

            var amount = (ulong)new Random().Next(100, int.MaxValue);
            var request = await Indy.Payments.BuildMintRequestAsync(Context.Wallet, Trustee.Did,
                new[] { new { recipient = address.Address, amount = amount } }.ToJson(), null);

            var mintResponse = await TrusteeMultiSignAndSubmitRequestAsync(request.Result);

            var totalAmount = await Policy.HandleResult<PaymentAmount>(x => x.Value == 0)
                .WaitAndRetryAsync(5, x => TimeSpan.FromSeconds(2))
                .ExecuteAsync(() => paymentService.GetBalanceAsync(Context, address));

            Assert.Equal(totalAmount.Value, amount);
        }

        [Fact(DisplayName = "Get transaction fees from ledger")]
        public async Task GetTransactionFeesAsync()
        {
            var paymentService = Host.Services.GetService<IPaymentService>();
            var fees = await paymentService.GetTransactionFeesAsync(Context);

            Assert.NotNull(fees);
        }

        [Fact(DisplayName = "Transfer funds between Sovrin addresses")]
        public async Task TransferFundsAsync()
        {
            //var address2 = await paymentService.CreatePaymentAddressAsync(Context, new PaymentAddressConfiguration
            //{
            //    AccountId = "000000000000000000000000Account1"
            //});

            //var paymentRecord = new PaymentRecord
            //{
            //    Address = address2.Address,
            //    Amount = (ulong)new Random().Next(100)
            //};
            //await paymentService.MakePaymentAsync(Context, paymentRecord, address);
        }

        /*
         * ''' txn codes 
            DOMAIN LEDGER
                NYM, 1
                ATTRIB, 100
                SCHEMA, 101
                CRED_DEF, 102
                REVOC_REG_DEF, 113
                REVOC_REG_ENTRY, 114
            PAYMENT LEDGER
                XFER_PUBLIC, 10001 '''
        */

        [Fact(DisplayName = "Set transaction fees")]
        public async Task SetTransactionFees()
        {
            var request = await Indy.Payments.BuildSetTxnFeesRequestAsync(Context.Wallet, Trustee.Did, TokenConfiguration.MethodName,
                new Dictionary<string, ulong>
                {
                    { "101", 0 }
                }.ToJson());
            var response = await TrusteeMultiSignAndSubmitRequestAsync(request);
            var jResponse = JObject.Parse(response);

            Assert.Equal(jResponse["op"].ToString(), "REPLY");
        }
    }
}
