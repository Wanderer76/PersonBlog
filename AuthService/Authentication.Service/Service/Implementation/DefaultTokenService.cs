using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared;
using Shared.Persistence;
using Shared.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;

[assembly: InternalsVisibleTo("Authentication.Test")]

namespace Authentication.Service.Service.Implementation
{
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

        public AuthResponse GenerateToken(AppUser user)
        {
            CreateTokenForUser(user, out var accessToken, out var refreshToken);

            var accessTokenModel = accessToken.ToTokenModel();
            var refreshTokenModel = refreshToken.ToTokenModel();
            var (jwtAccess, jwtRefresh) = JwtUtils.GetJwtTokens(accessTokenModel, refreshTokenModel);
            return new AuthResponse
            {
                AccessToken = jwtAccess,
                RefreshToken = jwtRefresh
            };
        }

        private void CreateTokenForUser(AppUser user, out Token accessToken, out Token refreshToken)
        {
            accessToken = new Token
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                TokenType = TokenTypes.Access,
                Login = user.Login,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddMonths(1),
                RoleId = user.AppUserRoles.First().UserRoleId
            };
            refreshToken = new Token
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
        }

        public async Task ClearUserToken(string token)
        {

            var userId = GetTokenRepresentaion(token).UserId;

            await _context.Get<Token>()
                .Where(x => x.AppUserId == userId)
                .ExecuteDeleteAsync();
        }

        public TokenModel GetTokenRepresentaion(string token)
        {
            return JwtUtils.GetTokenRepresentaion(token);
        }
    }
}
