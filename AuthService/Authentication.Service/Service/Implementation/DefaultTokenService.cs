using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Persistence;
using Shared.Services;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Authentication.Test")]

namespace Authentication.Service.Service.Implementation
{
    internal class DefaultTokenService : ITokenService
    {
        private readonly IReadWriteRepository<IAuthEntity> _context;
        private readonly IHttpClientFactory _httpClientFactory;


        public DefaultTokenService(IReadWriteRepository<IAuthEntity> context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public bool Validate(string token)
        {
            var tokenRepresentation = JwtUtils.GetTokenRepresentaion(token);
            if (tokenRepresentation == null)
            {
                return false;
            }
            var now = DateTimeOffset.UtcNow;
            if (tokenRepresentation.ExpiredAt < now)
            {
                return false;
            }

            return true;
        }

        public async Task<AuthResponse> GenerateTokenAsync(AppUser user)
        {
            var (accessToken, refreshToken) = CreateTokenForUser(user);

            var blog = await _httpClientFactory.CreateClient("Blog").GetFromJsonAsync<Guid?>($"Blog/hasBlog/{user.Id}");

            var accessTokenModel = accessToken.ToTokenModel(blog);
            var refreshTokenModel = refreshToken.ToTokenModel(blog);
            var (jwtAccess, jwtRefresh) = JwtUtils.GetJwtTokens(accessTokenModel, refreshTokenModel);
            return new AuthResponse
            {
                AccessToken = jwtAccess,
                RefreshToken = jwtRefresh
            };
        }

        private (Token accessToken, Token refreshToken) CreateTokenForUser(AppUser user)
        {
            var accessToken = new Token
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                TokenType = TokenTypes.Access,
                Login = user.Login,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddDays(10),
                RoleId = user.AppUserRoles.First().UserRoleId
            };
            var refreshToken = new Token
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                TokenType = TokenTypes.Refresh,
                Login = user.Login,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddYears(4),
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
            return JwtUtils.GetTokenRepresentaion(token);
        }

        public AuthResponse GenerateToken(AppUser user, Dictionary<string, string> claims)
        {
            CreateTokenForUser(user);

            var blogId = claims.ContainsKey(AppClaimTypes.BlogId) ? Guid.Parse(claims[AppClaimTypes.BlogId]) : Guid.Empty;
            var roleId = claims.ContainsKey(AppClaimTypes.RoleId) ? Guid.Parse(claims[AppClaimTypes.RoleId]) : user.AppUserRoles.First().UserRoleId;
            var userId = claims.ContainsKey(AppClaimTypes.UserId) ? Guid.Parse(claims[AppClaimTypes.UserId]) : user.Id;
            var login = claims.ContainsKey(AppClaimTypes.Login) ? claims[AppClaimTypes.Login] : user.Login;

            var accessModel = new TokenModel
            {
                CreatedAt = DateTimeService.Now(),
                ExpiredAt = DateTimeOffset.UtcNow.AddMonths(1),
                BlogId = blogId,
                Login = login,
                RoleId = roleId,
                UserId = userId,
                Type = TokenTypes.Access,
            };

            var refreshModel = new TokenModel
            {
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
}
