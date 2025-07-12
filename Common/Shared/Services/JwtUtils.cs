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
                Id = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.Id).Value),
                RoleId = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.RoleId).Value),
                UserId = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.UserId).Value),
                Type = jwtToken.Claims.First(x => x.Type == AppClaimTypes.Type).Value,
                ExpiredAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.Claims.First(x => x.Type == "exp").Value)).ToUniversalTime(),
                Login = jwtToken.Claims.FirstOrDefault(s => s.Type == AppClaimTypes.Login).Value,
                BlogId = Guid.Parse(jwtToken.Claims.FirstOrDefault(s => s.Type == AppClaimTypes.BlogId).Value)
            };
        }

        public static (string access, string refresh) GetJwtTokens(TokenModel access, TokenModel refresh)
        {
            var accessClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.Id, access.Id.ToString()),
                new Claim(AppClaimTypes.RoleId, access.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,access.Login),
                new Claim(AppClaimTypes.UserId,access.UserId.ToString()),
                new Claim(AppClaimTypes.Type,access.Type),
                new Claim(AppClaimTypes.BlogId,access.BlogId.ToString()),
                new Claim(AppClaimTypes.ExpiredAt,access.ExpiredAt.ToString()),
            };
            var refreshClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.Id, refresh.Id.ToString()),
                new Claim(AppClaimTypes.RoleId, refresh.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,refresh.Login),
                new Claim(AppClaimTypes.UserId,refresh.UserId.ToString()),
                new Claim(AppClaimTypes.Type,refresh.Type),
                new Claim(AppClaimTypes.BlogId,refresh.BlogId.ToString()),
                new Claim(AppClaimTypes.ExpiredAt,refresh.ExpiredAt.ToString()),
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
