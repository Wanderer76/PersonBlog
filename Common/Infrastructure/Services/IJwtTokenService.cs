using Microsoft.IdentityModel.Tokens;
using Shared;
using Shared.Utils;
using Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Services;
public interface IJwtTokenService
{
    (string Access, string Refresh) GetJwtTokens(TokenModel access, TokenModel refresh);
    Result<TokenModel> GetTokenModel(string token);
}

internal sealed class DefauleJWtTokenService : IJwtTokenService
{
    public (string Access, string Refresh) GetJwtTokens(TokenModel access, TokenModel refresh)
    {
        var accessClaims = new List<Claim>
        {
            new(AppClaimTypes.Id, access.Id.ToString()),
            new(AppClaimTypes.RoleId, access.RoleId.ToString()),
            new(AppClaimTypes.Login,access.Login),
            new(AppClaimTypes.UserId,access.UserId.ToString()),
            new(AppClaimTypes.Type,access.Type),
            new(ContextClaimTypes.Type, access.ContextType ?? string.Empty),
            new(ContextClaimTypes.Id, access.ContextId.ToString()),
            new(AppClaimTypes.BlogId,
                (access.ContextType == UserContextTypes.Blog ? access.ContextId : Guid.Empty).ToString()),
            new(AppClaimTypes.ExpiredAt,access.ExpiredAt.ToString()),
        };

        var refreshClaims = new List<Claim>
        {
            new(AppClaimTypes.Id, refresh.Id.ToString()),
            new(AppClaimTypes.RoleId, refresh.RoleId.ToString()),
            new(AppClaimTypes.Login,refresh.Login),
            new(AppClaimTypes.UserId,refresh.UserId.ToString()),
            new(AppClaimTypes.Type,refresh.Type),
            new(ContextClaimTypes.Type, refresh.ContextType ?? string.Empty),
            new(ContextClaimTypes.Id, refresh.ContextId.ToString()),
            new(AppClaimTypes.BlogId,
                (refresh.ContextType == UserContextTypes.Blog ? refresh.ContextId : Guid.Empty).ToString()),
            new(AppClaimTypes.ExpiredAt,refresh.ExpiredAt.ToString()),
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
    public Result<TokenModel> GetTokenModel(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (handler.CanReadToken(token))
        {
            if (handler.ReadToken(token) is not JwtSecurityToken jwtToken)
            {
                return Result<TokenModel>.Failure(new Error("cannot read token"));
            }

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
                Login = jwtToken.Claims.FirstOrDefault(s => s.Type == AppClaimTypes.Login)!.Value,
                ContextType = contextType,
                ContextId = contextId
            };
        }
        return Result<TokenModel>.Failure(new Error("", "cannot read token"));
    }
}
