using System.Threading;
using System.Threading.Tasks;
using Hyperledger.Indy.WalletApi;
using Microsoft.IdentityModel.Tokens;

namespace AgentFramework.Crypto.Tests
{
    public class DidSignatureProvider : SignatureProvider
    {
        public Wallet Wallet { get; set; }
        private readonly SemaphoreSlim _slim = new SemaphoreSlim(0, 1);

        internal DidSignatureProvider(Wallet wallet, SecurityKey key)
        : this(key, "EdDSA")
        {
            Wallet = wallet;
        }

        public DidSignatureProvider(SecurityKey key, string algorithm) : base(key, algorithm)
        {
        }

        protected override void Dispose(bool disposing)
        {
            
        }

        public override byte[] Sign(byte[] input)
        {
            var key = (IndySecurityKey)Key;
            byte[] result = null;
            Task.Run(async () =>
            {
                try
                {
                    result = await Hyperledger.Indy.CryptoApi.Crypto.SignAsync(Wallet, key.KeyId, input);
                }
                finally
                {
                    _slim.Release();
                }
            });
            _slim.Wait();
            return result;
        }

        public override bool Verify(byte[] input, byte[] signature)
        {
            
            var key = (IndySecurityKey)Key;
            bool result = false;
            Task.Run(async () =>
            {
                try
                {
                    result = await Hyperledger.Indy.CryptoApi.Crypto.VerifyAsync(key.KeyId, input, signature);
                }
                finally
                {
                    _slim.Release();
                }
            });
            _slim.Wait();
            return result;
        }
    }
}