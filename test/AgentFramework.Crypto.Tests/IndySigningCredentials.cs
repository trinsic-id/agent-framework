using System;
using System.Security.Cryptography.X509Certificates;
using Hyperledger.Indy.WalletApi;
using Microsoft.IdentityModel.Tokens;

namespace AgentFramework.Crypto.Tests
{
    public class IndySigningCredentials : SigningCredentials
    {
        protected IndySigningCredentials(X509Certificate2 certificate) : base(certificate)
        {
            throw new Exception("Unsupported ctor");
        }

        protected IndySigningCredentials(X509Certificate2 certificate, string algorithm) : base(certificate, algorithm)
        {
            throw new Exception("Unsupported ctor");
        }

        public IndySigningCredentials(SecurityKey key, string algorithm) : base(key, algorithm)
        {
            throw new Exception("Unsupported ctor");
        }

        public IndySigningCredentials(SecurityKey key, string algorithm, string digest) : base(key, algorithm, digest)
        {
            throw new Exception("Unsupported ctor");
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        public IndySigningCredentials(IndySecurityKey key, Wallet wallet)
            : base(key, "EdDSA")
        {
            CryptoProviderFactory = new IndyCryptoProviderFactory(wallet);
        }
    }
}