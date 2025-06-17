using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Models;
using Shared.Services;

namespace Infrastructure.Interface
{
    public interface ICurrentUserService
    {
        Task<UserModel> GetUserSessionAsync();
        Task<Guid> UpdateUserSession(string token);
    }

    internal class DefaultUserSession : ICurrentUserService
    {
        private readonly ICacheService _cacheService;
        private readonly IHttpContextAccessor _contextAccessor;

        public DefaultUserSession(ICacheService cacheService, IHttpContextAccessor contextAccessor)
        {
            _cacheService = cacheService;
            _contextAccessor = contextAccessor;
        }

        public async Task<UserModel> GetUserSessionAsync()
        {
            var token = _contextAccessor.HttpContext.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];

            var tokenRepr = token == null ? null : JwtUtils.GetTokenRepresentaion(token);

            var sessionData = tokenRepr != null
                ? (await _cacheService.GetCachedDataAsync<UserModel>(new SessionKey(tokenRepr.UserId)))!
                : UserModel.AnonymousUser();

            if (tokenRepr != null && sessionData == null)
            {
                await RefreshSession(tokenRepr);
                sessionData = (await _cacheService.GetCachedDataAsync<UserModel>(new SessionKey(tokenRepr.UserId)));
            }
            return sessionData;
        }

        public async Task<Guid> UpdateUserSession(string token)
        {
            var tokenRepr = JwtUtils.GetTokenRepresentaion(token);
            return await RefreshSession(tokenRepr);
        }

        private async Task<Guid> RefreshSession(TokenModel token)
        {
            var data = await _cacheService.GetCachedDataAsync<UserModel>(new SessionKey(token.UserId))!;
            if (token != null)
            {
                data ??= new UserModel();
                data!.UserId = token.UserId;
                data.UserName = token.Login;
                data.BlogId = token.BlogId;
            }
            await _cacheService.SetCachedDataAsync(new SessionKey(token.UserId), data!, TimeSpan.FromHours(1));

            return token.UserId;
        }
    }
}
