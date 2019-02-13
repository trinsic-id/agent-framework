using Microsoft.IdentityModel.Tokens;

namespace AgentFramework.Crypto.Tests
{
    public class IndySecurityKey : SecurityKey
    {
        private readonly string _keyId;

        public IndySecurityKey(string verkey)
        {
            _keyId = verkey;
        }

        public override string KeyId => _keyId;

        public override int KeySize => 256;
    }
}