using Authentication.Domain.Entities;
using Authentication.Peristence;
using Authentication.Service.Models;
using Shared.Persistence;
using Shared.Utils;
using System.Text.Json;

namespace Authentication.Service.Service.Implementation
{
    internal class DefaultTokenService
    {
        private readonly IReadWriteRepository<IAuthEntity> _context;

        public DefaultTokenService(IReadWriteRepository<IAuthEntity> context)
        {
            _context = context;
        }

        public bool Validate(string token)
        {
            return true;
        }

        public AuthResponse GenerateToken(AppUser user)
        {
            var accessToken = new Token
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                TokenType = TokenTypes.Access,
                Login = user.Login,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddHours(1),
            };

            var refreshToken = new Token
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                TokenType = TokenTypes.Refresh,
                Login = user.Login,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddHours(1),
            };

            _context.Add(accessToken);
            _context.Add(refreshToken);

            var (jwtAccess, jwtRefresh) = JwtService.GetJwtTokens(accessToken, refreshToken);
            return new AuthResponse
            {
                AccessToken = jwtAccess,
                RefreshToken = jwtRefresh
            };
        }


        public TokenModel GetTokenRepresentaion(string token)
        {
            var tokenModel = JsonSerializer.Deserialize<TokenModel>(token);
            tokenModel.AssertFound("Не валидный токен");
            return tokenModel;
        }
    }
}
