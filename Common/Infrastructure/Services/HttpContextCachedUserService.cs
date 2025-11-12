using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Models;
using Shared.Services;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AuthTests")]

namespace Infrastructure.Services
{
    internal class HttpContextCachedUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ICacheService _cacheService;
        public HttpContextCachedUserService(IHttpContextAccessor contextAccessor, ICacheService cacheService)
        {
            _contextAccessor = contextAccessor;
            _cacheService = cacheService;
        }

        public async Task<UserModel> GetCurrentUserAsync()
        {
            var token = _contextAccessor.HttpContext!.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
            var tokenRepr = token == null ? null : JwtUtils.GetTokenRepresentaion(token);
            if (tokenRepr == null || tokenRepr.IsFailure)
                return UserModel.AnonymousUser();

            var key = new SessionKey(tokenRepr.Value.UserId);

            var data = await _cacheService.GetCachedDataAsync<UserModel>(key);
            
            if (data == null)
                return UserModel.AnonymousUser();

            return data;
        }
    }
}
