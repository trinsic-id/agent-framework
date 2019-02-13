using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using AgentFramework.Core.Extensions;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AgentFramework.Crypto.Tests
{
    public class JwtTests
    {
        private readonly string _config = $"{{\"id\":\"{Guid.NewGuid()}\"}}";
        private readonly string _creds = "{\"key\":\"test_wallet_key\"}";

        [Fact]
        public async Task IssueAndVerifyJwt()
        {
            string token;
            string pubkey;

            // Signing scope
            {
                await Wallet.CreateWalletAsync(_config, _creds);
                var wallet = await Wallet.OpenWalletAsync(_config, _creds);
                pubkey = await Hyperledger.Indy.CryptoApi.Crypto.CreateKeyAsync(wallet, "{}");

                var securityKey = new IndySecurityKey(pubkey);
                var signingCredentials = new IndySigningCredentials(securityKey, wallet);

                var now = DateTime.Now;
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.CreateJwtSecurityToken(
                    issuer: "me",
                    audience: "you",
                    subject: null,
                    notBefore: now,
                    expires: now.AddMinutes(30),
                    issuedAt: now,
                    signingCredentials: signingCredentials);

                token = tokenHandler.WriteToken(jwtToken);

                Assert.NotNull(token);
                Assert.Equal(3, token.Split(".").Length);

                await wallet.CloseAsync();
                await Wallet.DeleteWalletAsync(_config, _creds);
            }

            // Verification scope
            {
                var settings = new TokenValidationParameters
                {
                    ValidIssuer = "me",
                    ValidAudience = "you",
                    IssuerSigningKey = new IndySecurityKey(pubkey),
                    CryptoProviderFactory = new IndyCryptoProviderFactory()
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var claims = tokenHandler.ValidateToken(token, settings, out var securityToken);

                Assert.Equal("me", securityToken.Issuer);
                Assert.NotNull(claims);
            }
        }
    }
}
