using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Shared.Models;
using Shared.Services;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AuthTests")]

namespace Authentication.Contract.Services
{
    internal class HttpContextCachedUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        public HttpContextCachedUserService(IHttpContextAccessor contextAccessor, ICacheService cacheService, ICurrentUserService currentUserService)
        {
            _contextAccessor = contextAccessor;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
        }

        public async Task<UserModel> GetCurrentUserAsync()
        {
            var token = _contextAccessor.HttpContext!.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
            var tokenRepr = token == null ? null : JwtUtils.GetTokenRepresentaion(token);
            if (tokenRepr == null || tokenRepr.IsFailure)
                return UserModel.AnonymousUser();

            var key = new SessionKey(tokenRepr.Value.UserId);
            var data = await _cacheService.GetOrAddDataAsync(key, _currentUserService.GetCurrentUserAsync);
            return data;
        }
    }
}
