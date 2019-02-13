using Hyperledger.Indy.WalletApi;
using Microsoft.IdentityModel.Tokens;

namespace AgentFramework.Crypto.Tests
{
    public class IndyCryptoProviderFactory : CryptoProviderFactory
    {
        public Wallet Wallet { get; }

        public IndyCryptoProviderFactory(Wallet wallet)
        {
            Wallet = wallet;
        }

        public IndyCryptoProviderFactory()
        {
        }

        public override SignatureProvider CreateForSigning(SecurityKey key, string algorithm)
        {
            return new DidSignatureProvider(Wallet, key);
        }

        public override SignatureProvider CreateForVerifying(SecurityKey key, string algorithm)
        {
            return new DidSignatureProvider(Wallet, key);
        }

        public override bool IsSupportedAlgorithm(string algorithm, SecurityKey key)
        {
            return algorithm == "EdDSA" && key is IndySecurityKey;
        }
    }
}