using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Models;
using Shared.Services;

namespace Infrastructure.Services
{
    internal class HttpContextUserService : ICurrentUserService
    {
        private  UserModel? userModel;
        private readonly IHttpContextAccessor _contextAccessor;

        public HttpContextUserService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<UserModel> GetCurrentUserAsync()
        {
            if (userModel != null)
            {
                return userModel;
            }

            var token = _contextAccessor.HttpContext.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];

            var tokenRepr = token == null ? null : JwtUtils.GetTokenRepresentaion(token);

            var sessionData = tokenRepr != null
                ? new UserModel
                {
                    UserId = tokenRepr.UserId,
                    UserName = tokenRepr.Login,
                    BlogId = tokenRepr.BlogId,
                }
                : UserModel.AnonymousUser();
            userModel = sessionData;
            return userModel;
        }
    }
}
