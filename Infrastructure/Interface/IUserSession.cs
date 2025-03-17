using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Shared.Models;
using Shared.Services;

namespace Infrastructure.Interface
{
    public interface IUserSession
    {
        Task<UserSession> GetUserSessionAsync();
        Task UpdateUserSession(string sessionId, string? token = null);
    }
    internal class DefaultUserSession : IUserSession
    {
        private readonly ICacheService _cacheService;
        private readonly IHttpContextAccessor _contextAccessor;

        public DefaultUserSession(ICacheService cacheService, IHttpContextAccessor contextAccessor)
        {
            _cacheService = cacheService;
            _contextAccessor = contextAccessor;
        }

        public async Task<UserSession> GetUserSessionAsync()
        {
            var hasSession = _contextAccessor.HttpContext.Request.Cookies.TryGetValue(SessionKey.Key, out var session);
            return hasSession
                ? (await _cacheService.GetCachedDataAsync<UserSession>(new SessionKey(Guid.Parse(session!))))!
                : throw new Exception();
        }

        public async Task UpdateUserSession(string session, string? token = null)
        {
            token = _contextAccessor.HttpContext.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
            await RefreshSession(session, token);
        }

        private async Task RefreshSession(string? session, string? token = null)
        {
            if (session != null)
            {
                var data = await _cacheService.GetCachedDataAsync<UserSession>($"{new SessionKey()}:{session}")!;
                if (data == null)
                {
                    await _cacheService.RemoveCachedDataAsync(new SessionKey(session));
                    _contextAccessor.HttpContext?.Response.Cookies.Delete(new SessionKey());
                }
                else
                {
                    if (token != null)
                    {
                        data!.UserId = JwtUtils.GetTokenRepresentaion(token).UserId;
                    }
                    await _cacheService.SetCachedDataAsync(new SessionKey(session), data!, TimeSpan.FromHours(1));
                }
            }
            else
            {
                var sessionId = GuidService.GetNewGuid();
                await _cacheService.SetCachedDataAsync(new SessionKey(sessionId), new UserSession { SessionId = sessionId }, TimeSpan.FromHours(1));
                _contextAccessor.HttpContext?.Response.Cookies.Append(SessionKey.Key, sessionId.ToString(), new CookieOptions
                {
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true
                });
            }
        }
    }
}
