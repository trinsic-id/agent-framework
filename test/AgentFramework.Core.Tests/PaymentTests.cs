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
using System.Linq;
using AgentFramework.Core.Models.Ledger;

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
                new[] { new IndyPaymentOutputSource { Recipient = address.Address, Amount = amount } }.ToJson(), null);

            var mintResponse = await TrusteeMultiSignAndSubmitRequestAsync(request.Result);

            await paymentService.GetBalanceAsync(Context, address);

            Assert.Equal(address.Balance, amount);
        }

        [Fact(DisplayName = "Get transaction fees from ledger")]
        public async Task GetTransactionFeesAsync()
        {
            var paymentService = Host.Services.GetService<IPaymentService>();
            var fees = await paymentService.GetTransactionFeesAsync(Context);

            Assert.NotNull(fees);
        }

        [Fact(DisplayName = "Create schema")]
        public async Task CreateSchemaWithFeesAsync()
        {
            var provService = Host.Services.GetService<IProvisioningService>();
            var schemaService = Host.Services.GetService<ISchemaService>();
            var ledgerService = Host.Services.GetService<ILedgerService>();

            var prov = await provService.GetProvisioningAsync(Context.Wallet);

            var request = await Ledger.BuildNymRequestAsync(Trustee.Did, prov.IssuerDid, prov.IssuerVerkey, null, "TRUST_ANCHOR");
            var response = await Ledger.SignAndSubmitRequestAsync(await Context.Pool, Context.Wallet, Trustee.Did, request);

            await schemaService.CreateSchemaAsync(await Context.Pool, Context.Wallet, $"test{Guid.NewGuid().ToString("N")}", "1.0", new[] { "name-one" });
        }

        [Fact(DisplayName = "Transfer funds between Sovrin addresses")]
        public async Task TransferFundsAsync()
        {
            // Generate from address
            var paymentService = Host.Services.GetService<IPaymentService>();
            var recordService = Host.Services.GetService<IWalletRecordService>();
            var addressFrom = await paymentService.CreatePaymentAddressAsync(Context);

            // Mint tokens to the address to fund initially
            var request = await Indy.Payments.BuildMintRequestAsync(Context.Wallet, Trustee.Did,
                new[] { new { recipient = addressFrom.Address, amount = 15 } }.ToJson(), null);
            await TrusteeMultiSignAndSubmitRequestAsync(request.Result);

            // Generate destination address
            var addressTo = await paymentService.CreatePaymentAddressAsync(Context);

            // Create payment record and make payment
            var paymentRecord = new PaymentRecord
            {
                Address = addressTo.Address,
                Amount = 10
            };
            await recordService.AddAsync(Context.Wallet, paymentRecord);
            await paymentService.MakePaymentAsync(Context, paymentRecord, addressFrom);

            var fee = await paymentService.GetTransactionFeeAsync(Context, TransactionTypes.XFER_PUBLIC);

            await paymentService.GetBalanceAsync(Context, addressFrom);
            await paymentService.GetBalanceAsync(Context, addressTo);

            Assert.Equal(10UL, addressTo.Balance);
            Assert.Equal(5UL - fee, addressTo.Balance);
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

        [Fact(DisplayName = "Get auth rules from the ledger")]
        public async Task GetAuthRules()
        {
            var ledgerService = Host.Services.GetService<ILedgerService>();

            var rules = await ledgerService.LookupAuthorizationRulesAsync(await Context.Pool);

            Assert.NotNull(rules);
            Assert.True(rules.Any());
            Assert.True(rules.Count > 0);
        }

        [Fact(DisplayName = "Get transaction fees for a specific type")]
        public async Task GetTxnFeesAsync()
        {
            var paymentService = Host.Services.GetService<IPaymentService>();
            _ = await paymentService.GetTransactionFeeAsync(Context, TransactionTypes.SCHEMA);

            Assert.True(true);
        }

        /*
              {
        "auth_type": "101",
        "new_value": "*",
        "field": "*",
        "auth_action": "ADD",
        "constraint": {
          "constraint_id": "OR",
          "auth_constraints": [
            {
              "metadata": {
                
              },
              "constraint_id": "ROLE",
              "need_to_be_owner": false,
              "role": "0",
              "sig_count": 1
            },
            {
              "metadata": {
                
              },
              "constraint_id": "ROLE",
              "need_to_be_owner": false,
              "role": "2",
              "sig_count": 1
            },
            {
              "metadata": {
                
              },
              "constraint_id": "ROLE",
              "need_to_be_owner": false,
              "role": "101",
              "sig_count": 1
            }
          ]
        }
      },
             */

        [Fact(DisplayName = "Set auth rules for 101 (SCHEMA) transactions to use fees")]
        public async Task SetFeesForSchemaTransactionsAsync()
        {
            var request = await Indy.Payments.BuildSetTxnFeesRequestAsync(Context.Wallet, Trustee.Did, TokenConfiguration.MethodName,
                new Dictionary<string, ulong>
                {
                                { "fees_for_schema", 10 }
                }.ToJson());
            var response = await TrusteeMultiSignAndSubmitRequestAsync(request);

            request = await Ledger.BuildAuthRuleRequestAsync(Trustee.Did, "101", "ADD", "*", "*", "*", new
            {
                constraint_id = "OR",
                auth_constraints = new[] {
                    new {
                        metadata = new {
                            fees = "fees_for_schema"
                        },
                      constraint_id = "ROLE",
                      need_to_be_owner = false,
                      role = "0",
                      sig_count = 1
                    },
                    new {
                        metadata= new {
                            fees = "fees_for_schema"
                        },
                      constraint_id= "ROLE",
                      need_to_be_owner= false,
                      role= "2",
                      sig_count= 1
                    },
                    new {
                        metadata= new {
                            fees = "fees_for_schema"
                        },
                      constraint_id= "ROLE",
                      need_to_be_owner= false,
                      role= "101",
                      sig_count= 1
                    }
                }
            }.ToJson());
            response = await TrusteeMultiSignAndSubmitRequestAsync(request);

            Console.WriteLine(response);
        }

        [Fact(DisplayName = "Set transaction fees")]
        public async Task SetTransactionFees()
        {
            var request = await Indy.Payments.BuildSetTxnFeesRequestAsync(Context.Wallet, Trustee.Did, TokenConfiguration.MethodName,
                new Dictionary<string, ulong>
                {
                    { "101", 1 },
                    { "10001", 2 }
                }.ToJson());
            var response = await TrusteeMultiSignAndSubmitRequestAsync(request);
            var jResponse = JObject.Parse(response);

            Assert.Equal(jResponse["op"].ToString(), "REPLY");

            // Cleanup and revert back fees to 0
            request = await Indy.Payments.BuildSetTxnFeesRequestAsync(Context.Wallet, Trustee.Did, TokenConfiguration.MethodName,
                new Dictionary<string, ulong>
                {
                    { "101", 0 },
                    { "10001", 0 }
                }.ToJson());
            await TrusteeMultiSignAndSubmitRequestAsync(request);
        }
    }
}
