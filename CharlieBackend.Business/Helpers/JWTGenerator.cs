using System;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using CharlieBackend.Business.Options;
using System.IdentityModel.Tokens.Jwt;
using CharlieBackend.Core.DTO.Account;
using CharlieBackend.Business.Helpers;
using Microsoft.Extensions.Options;

namespace CharlieBackend.Business.Helpers
{
    public class JWTGenerator : IJWTGenerator
    {
        private AuthOptions _authOptions;

        public JWTGenerator(IOptions<AuthOptions> authOptions)
        {
            _authOptions = authOptions.Value;
        }

        public string GenerateEncodedJWT(AccountDto account)
        {

            var jwt = GenerateJWT(account);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        private JwtSecurityToken GenerateJWT(AccountDto account)
        {
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                        issuer: _authOptions.ISSUER,
                        audience: _authOptions.AUDIENCE,
                        notBefore: now,
                        claims: new List<Claim>
                        {
                            new Claim(ClaimsIdentity.DefaultRoleClaimType,
                                    account.Role.ToString()),
                            new Claim(ClaimConstants.IdClaim, account.Id.ToString()),
                            new Claim(ClaimConstants.EmailClaim, account.Email),
                            new Claim(ClaimConstants.AccountClaim, account.Id.ToString())
                        },
                        expires: now.Add(TimeSpan.FromMinutes(_authOptions.LIFETIME)),
                        signingCredentials:
                                new SigningCredentials(
                                        _authOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
                        );

            return jwt;
        }
    }
}
