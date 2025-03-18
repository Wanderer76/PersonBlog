using Infrastructure.Services;
using Shared.Models;

namespace Infrastructure.Extensions
{
    public static class UserSessionExtensions
    {
        public static Task<UserSession?> GetUserSessionCachedAsync(this ICacheService cacheService, Guid sessionId)
        {
            return cacheService.GetCachedDataAsync<UserSession>(new SessionKey(sessionId));
        }
    }
}
