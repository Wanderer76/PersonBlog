using Authentication.Domain.Interfaces.Models;
using Authentication.Domain.Interfaces;
using Infrastructure.Cache.Services;
using Microsoft.AspNetCore.Http;
using Shared.Services;

namespace Authentication.Service.Service.Implementation
{
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
            var hasSession = _contextAccessor.HttpContext.Request.Cookies.TryGetValue("sessionId", out var session);
            return hasSession
                ? await _cacheService.GetCachedDataAsync<UserSession>($"Session:{session}")
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
                var data = await _cacheService.GetCachedDataAsync<UserSession>($"Session:{session}")!;
                if (data == null)
                {
                    await _cacheService.RemoveCachedDataAsync($"Session:{session}");
                    _contextAccessor.HttpContext?.Response.Cookies.Delete("sessionId");
                }
                else
                {
                    if (token != null)
                    {
                        data!.UserId = JwtUtils.GetTokenRepresentaion(token).UserId;
                    }
                    await _cacheService.SetCachedDataAsync($"Session:{session}", data!, TimeSpan.FromHours(1));
                }
            }
            else
            {
                var sessionId = GuidService.GetNewGuid().ToString();
                await _cacheService.SetCachedDataAsync($"Session:{sessionId}", new UserSession(), TimeSpan.FromHours(1));
                _contextAccessor.HttpContext?.Response.Cookies.Append("sessionId", sessionId, new CookieOptions
                {
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true
                });
            }
        }
    }
}