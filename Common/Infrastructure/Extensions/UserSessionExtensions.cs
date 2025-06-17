using Infrastructure.Services;
using Shared.Models;

namespace Infrastructure.Extensions
{
    public static class UserSessionExtensions
    {
        public static Task<UserModel?> GetUserSessionCachedAsync(this ICacheService cacheService, Guid sessionId)
        {
            return cacheService.GetCachedDataAsync<UserModel>(new SessionKey(sessionId));
        }
    }
}
