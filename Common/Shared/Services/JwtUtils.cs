using Microsoft.IdentityModel.Tokens;
using Shared.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Shared.Models;

namespace Shared.Services
{
    public static class JwtUtils
    {
        public static Result<TokenModel> GetTokenRepresentaion(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                var contextType = jwtToken.Claims.FirstOrDefault(x => x.Type == ContextClaimTypes.Type)?.Value;
                var contextIdValue = jwtToken.Claims.FirstOrDefault(x => x.Type == ContextClaimTypes.Id)?.Value;
                var legacyBlogIdValue = jwtToken.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.BlogId)?.Value;
                var contextId = Guid.TryParse(contextIdValue, out var parsedContextId)
                    ? parsedContextId
                    : Guid.TryParse(legacyBlogIdValue, out var parsedBlogId)
                        ? parsedBlogId
                        : Guid.Empty;

                if (string.IsNullOrWhiteSpace(contextType) && contextId != Guid.Empty)
                    contextType = UserContextTypes.Blog;

                return new TokenModel
                {
                    Id = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.Id).Value),
                    RoleId = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.RoleId).Value),
                    UserId = Guid.Parse(jwtToken.Claims.First(x => x.Type == AppClaimTypes.UserId).Value),
                    Type = jwtToken.Claims.First(x => x.Type == AppClaimTypes.Type).Value,
                    ExpiredAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.Claims.First(x => x.Type == "exp").Value)).ToUniversalTime(),
                    Login = jwtToken.Claims.FirstOrDefault(s => s.Type == AppClaimTypes.Login).Value,
                    ContextType = contextType,
                    ContextId = contextId
                };
            }
            return Result<TokenModel>.Failure(new Error("", "cannot read token"));
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
                new Claim(ContextClaimTypes.Type, access.ContextType ?? string.Empty),
                new Claim(ContextClaimTypes.Id, access.ContextId.ToString()),
                new Claim(AppClaimTypes.BlogId,
                    (access.ContextType == UserContextTypes.Blog ? access.ContextId : Guid.Empty).ToString()),
                new Claim(AppClaimTypes.ExpiredAt,access.ExpiredAt.ToString()),
                //new Claim(AppClaimTypes.Name,access.Name),
            };
            var refreshClaims = new List<Claim>
            {
                new Claim(AppClaimTypes.Id, refresh.Id.ToString()),
                new Claim(AppClaimTypes.RoleId, refresh.RoleId.ToString()),
                new Claim(AppClaimTypes.Login,refresh.Login),
                new Claim(AppClaimTypes.UserId,refresh.UserId.ToString()),
                new Claim(AppClaimTypes.Type,refresh.Type),
                new Claim(ContextClaimTypes.Type, refresh.ContextType ?? string.Empty),
                new Claim(ContextClaimTypes.Id, refresh.ContextId.ToString()),
                new Claim(AppClaimTypes.BlogId,
                    (refresh.ContextType == UserContextTypes.Blog ? refresh.ContextId : Guid.Empty).ToString()),
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
