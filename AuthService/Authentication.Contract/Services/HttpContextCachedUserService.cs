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
        private readonly IJwtTokenService _jwtTokenService;
        public HttpContextCachedUserService(IHttpContextAccessor contextAccessor, ICacheService cacheService, ICurrentUserService currentUserService, IJwtTokenService jwtTokenService)
        {
            _contextAccessor = contextAccessor;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<UserModel> GetCurrentUserAsync()
        {
            var token = _contextAccessor.HttpContext!.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
            var tokenRepr = token == null ? null : _jwtTokenService.GetTokenModel(token);
            if (tokenRepr == null || tokenRepr.IsFailure)
                return UserModel.AnonymousUser();

            if (tokenRepr.IsSuccess && tokenRepr.Value.ExpiredAt < DateTimeService.Now())
                return UserModel.AnonymousUser();

            var key = new SessionKey(tokenRepr.Value.UserId);
            var data = await _cacheService.GetOrAddDataAsync(key, _currentUserService.GetCurrentUserAsync, 1);
            return data;
        }
    }
}
