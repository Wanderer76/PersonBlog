using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Shared.Services
{
    public static class JwtUtils
    {
        public static TokenModel GetTokenRepresentaion(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            return new TokenModel
            {
                RoleId = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.RoleId).Value),
                UserId = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.UserId).Value),
                Type = jwtToken.Claims.First(x => x.Type == AppClaimTypes.Type).Value,
                ExpiredAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.Claims.First(x => x.Type == "exp").Value)).ToUniversalTime(),
            };
        }

        public static (string access, string refresh) GetJwtTokens(TokenModel access, TokenModel refresh)
        {
            var accessClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.RoleId, access.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,access.Login),
                new Claim(AppClaimTypes.UserId,access.UserId.ToString()),
                new Claim(AppClaimTypes.Type,access.Type)
            };
            var refreshClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.RoleId, refresh.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,refresh.Login),
                new Claim(AppClaimTypes.UserId,refresh.UserId.ToString()),
                new Claim(AppClaimTypes.Type,refresh.Type)
            };

            var jwt = new JwtSecurityToken(
                    claims: accessClaims,
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    expires: access.ExpiredAt.UtcDateTime,
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var refreshJwt = new JwtSecurityToken(
                    claims: refreshClaims,
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    expires: refresh.ExpiredAt.UtcDateTime,
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            var encodedRefreshJwt = new JwtSecurityTokenHandler().WriteToken(refreshJwt);

            return (encodedJwt, encodedRefreshJwt);
        }
    }
}
