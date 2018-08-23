using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Streetcred.Sdk.Model.Wallets;

namespace Agency.Web.Utils
{
    public class WalletUtils
    {
        public static WalletConfiguration Configuration = new WalletConfiguration {Id = "DefaultWallet"};
        public static WalletCredentials Credentials = new WalletCredentials {Key = "SecretKeyPhrase"};
    }
}
