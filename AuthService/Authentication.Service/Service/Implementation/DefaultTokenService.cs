using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Authentication.Service.Models.Options;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Persistence;
using Shared.Services;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AuthTests")]

namespace Authentication.Service.Service.Implementation;

internal sealed class DefaultTokenService : ITokenService
{
    private readonly IReadWriteRepository<IAuthEntity> _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly TokenOptions tokenOptions;

    public DefaultTokenService(IReadWriteRepository<IAuthEntity> context, IJwtTokenService jwtTokenService, TokenOptions tokenOptions)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        this.tokenOptions = tokenOptions;
    }

    public bool Validate(string token)
    {
        var tokenRepresentation = GetTokenRepresentation(token);
        if (tokenRepresentation.IsFailure)
        {
            return false;
        }
        var now = DateTimeOffset.UtcNow;
        if (tokenRepresentation.Value.ExpiredAt < now)
        {
            return false;
        }

        return true;
    }

    public async Task<AuthResponse> GenerateTokenAsync(AppUser user)
    {
        var (accessToken, refreshToken) = CreateTokenForUser(user);
        var profile = await _context.Get<AppProfile>()
            .Where(x => x.UserId == user.Id)
            .FirstOrDefaultAsync();

        var accessTokenModel = accessToken.ToTokenModel(user);
        var refreshTokenModel = refreshToken.ToTokenModel(user);
        var (jwtAccess, jwtRefresh) = _jwtTokenService.GetJwtTokens(accessTokenModel, refreshTokenModel);
        return new AuthResponse
        {
            AccessToken = jwtAccess,
            RefreshToken = jwtRefresh
        };
    }

    private (Token accessToken, Token refreshToken) CreateTokenForUser(AppUser user)
    {
        var now = DateTimeService.Now();
        var accessToken = new Token
        {
            Id = Guid.NewGuid(),
            AppUserId = user.Id,
            TokenType = TokenTypes.Access,
            Login = user.Login,
            CreatedAt = now,
            ExpiredAt = now.AddMinutes(tokenOptions.AccessTokenExpiredInMinutes),
            RoleId = user.AppUserRoles.First().UserRoleId,
        };
        var refreshToken = new Token
        {
            Id = Guid.NewGuid(),
            AppUserId = user.Id,
            TokenType = TokenTypes.Refresh,
            Login = user.Login,
            CreatedAt = now,
            ExpiredAt = now.AddMinutes(tokenOptions.AccessTokenExpiredInMinutes),
            RoleId = user.AppUserRoles.First().UserRoleId
        };
        _context.Add(accessToken);
        _context.Add(refreshToken);

        return (accessToken, refreshToken);
    }

    public async Task ClearUserToken(string token)
    {
        var user = GetTokenRepresentation(token);

        if (user.IsSuccess)
        {
            var userId = user.Value.UserId;
            await _context.Get<Token>()
                .Where(x => x.AppUserId == userId)
                .ExecuteDeleteAsync();
        }
    }

    public Result<TokenModel> GetTokenRepresentation(string token)
    {
        return _jwtTokenService.GetTokenModel(token);
    }

    public AuthResponse GenerateToken(AppUser user, Dictionary<string, string> claims)
    {
        var (access, refresh) = CreateTokenForUser(user);

        var tokenId = claims.ContainsKey(AppClaimTypes.Id) ? Guid.Parse(claims[AppClaimTypes.Id]) : Guid.Empty;
        var contextType = claims.GetValueOrDefault(Shared.Models.ContextClaimTypes.Type);
        var contextId = claims.TryGetValue(Shared.Models.ContextClaimTypes.Id, out var contextIdValue)
            ? Guid.Parse(contextIdValue)
            : claims.TryGetValue(AppClaimTypes.BlogId, out var blogIdValue)
                ? Guid.Parse(blogIdValue)
                : Guid.Empty;
        contextType ??= contextId == Guid.Empty ? null : Shared.Models.UserContextTypes.Blog;
        var roleId = claims.ContainsKey(AppClaimTypes.RoleId) ? Guid.Parse(claims[AppClaimTypes.RoleId]) : user.AppUserRoles.First().UserRoleId;
        var userId = claims.ContainsKey(AppClaimTypes.UserId) ? Guid.Parse(claims[AppClaimTypes.UserId]) : user.Id;
        var login = claims.ContainsKey(AppClaimTypes.Login) ? claims[AppClaimTypes.Login] : user.Login;
        var name = claims.ContainsKey(AppClaimTypes.Name) ? claims[AppClaimTypes.Name] : null;

        var accessModel = new TokenModel
        {
            Id = access.Id,
            CreatedAt = access.CreatedAt,
            ExpiredAt = access.ExpiredAt,
            ContextType = contextType,
            ContextId = contextId,
            Login = login,
            RoleId = roleId,
            UserId = userId,
            Type = TokenTypes.Access,
        };

        var refreshModel = new TokenModel
        {
            Id = tokenId,
            CreatedAt = refresh.CreatedAt,
            ExpiredAt = refresh.ExpiredAt,
            ContextType = contextType,
            ContextId = contextId,
            Login = login,
            RoleId = roleId,
            UserId = userId,
            Type = TokenTypes.Refresh,
        };
        var (jwtAccess, jwtRefresh) = _jwtTokenService.GetJwtTokens(accessModel, refreshModel);

        return new AuthResponse
        {
            AccessToken = jwtAccess,
            RefreshToken = jwtRefresh
        };
    }
}
