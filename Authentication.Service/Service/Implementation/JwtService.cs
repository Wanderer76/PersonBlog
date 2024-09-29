using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Authentication.Service.Service.Implementation
{
    public class JwtService
    {
        public static (string access, string refresh) GetJwtTokens(Token access, Token refresh)
        {
            var accessClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.RoleId, access.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,access.Login),
                new Claim(AppClaimTypes.UserId,access.AppUserId.ToString()),
                new Claim(AppClaimTypes.Type,access.TokenType)
            };
            var refreshClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.RoleId, refresh.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,refresh.Login),
                new Claim(AppClaimTypes.UserId,refresh.AppUserId.ToString()),
                new Claim(AppClaimTypes.Type,refresh.TokenType)
            };

            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: access.CreatedAt.DateTime,
                    claims: accessClaims,
                    expires: access.ExpiredAt.DateTime,
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var refreshJwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: refresh.CreatedAt.DateTime,
                    claims: refreshClaims,
                    expires: refresh.ExpiredAt.DateTime,
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            var encodedRefreshJwt = new JwtSecurityTokenHandler().WriteToken(refreshJwt);
            
            return (encodedJwt, encodedRefreshJwt);
        }
    }
}
