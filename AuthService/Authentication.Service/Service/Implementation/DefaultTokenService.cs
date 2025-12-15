using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Persistence;
using Shared.Services;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AuthTests")]

namespace Authentication.Service.Service.Implementation;

internal class DefaultTokenService : ITokenService
{
    private readonly IReadWriteRepository<IAuthEntity> _context;

    public DefaultTokenService(IReadWriteRepository<IAuthEntity> context)
    {
        _context = context;
    }

    public bool Validate(string token)
    {
        var tokenRepresentation = JwtUtils.GetTokenRepresentaion(token);
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

        var accessTokenModel = accessToken.ToTokenModel(profile);
        var refreshTokenModel = refreshToken.ToTokenModel(profile);
        var (jwtAccess, jwtRefresh) = JwtUtils.GetJwtTokens(accessTokenModel, refreshTokenModel);
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
            ExpiredAt = now.AddMinutes(5),
            RoleId = user.AppUserRoles.First().UserRoleId,
        };
        var refreshToken = new Token
        {
            Id = Guid.NewGuid(),
            AppUserId = user.Id,
            TokenType = TokenTypes.Refresh,
            Login = user.Login,
            CreatedAt = now,
            ExpiredAt = now.AddMinutes(60),
            RoleId = user.AppUserRoles.First().UserRoleId
        };
        _context.Add(accessToken);
        _context.Add(refreshToken);

        return (accessToken, refreshToken);
    }

    public async Task ClearUserToken(string token)
    {

        var userId = GetTokenRepresentation(token).UserId;

        await _context.Get<Token>()
            .Where(x => x.AppUserId == userId)
            .ExecuteDeleteAsync();
    }

    public TokenModel GetTokenRepresentation(string token)
    {
        var result = JwtUtils.GetTokenRepresentaion(token);
        if (result.IsFailure)
            return null;
        return result.Value;
    }

    public AuthResponse GenerateToken(AppUser user, Dictionary<string, string> claims)
    {
        CreateTokenForUser(user);

        var tokenId = claims.ContainsKey(AppClaimTypes.Id) ? Guid.Parse(claims[AppClaimTypes.Id]) : Guid.Empty;
        var blogId = claims.ContainsKey(AppClaimTypes.BlogId) ? Guid.Parse(claims[AppClaimTypes.BlogId]) : Guid.Empty;
        var roleId = claims.ContainsKey(AppClaimTypes.RoleId) ? Guid.Parse(claims[AppClaimTypes.RoleId]) : user.AppUserRoles.First().UserRoleId;
        var userId = claims.ContainsKey(AppClaimTypes.UserId) ? Guid.Parse(claims[AppClaimTypes.UserId]) : user.Id;
        var login = claims.ContainsKey(AppClaimTypes.Login) ? claims[AppClaimTypes.Login] : user.Login;
        var name = claims.ContainsKey(AppClaimTypes.Name) ? claims[AppClaimTypes.Name] : null;

        var accessModel = new TokenModel
        {
            Id = tokenId,
            CreatedAt = DateTimeService.Now(),
            ExpiredAt = DateTimeOffset.UtcNow.AddMonths(1),
            BlogId = blogId,
            Login = login,
            RoleId = roleId,
            UserId = userId,
            Type = TokenTypes.Access,
            Name = name
        };

        var refreshModel = new TokenModel
        {
            Id = tokenId,
            CreatedAt = DateTimeService.Now(),
            ExpiredAt = DateTimeService.Now().AddYears(4),
            BlogId = blogId,
            Login = login,
            RoleId = roleId,
            UserId = userId,
            Type = TokenTypes.Refresh,
        };
        var (jwtAccess, jwtRefresh) = JwtUtils.GetJwtTokens(accessModel, refreshModel);
        return new AuthResponse
        {
            AccessToken = jwtAccess,
            RefreshToken = jwtRefresh
        };
    }
}
