using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Models;
using Shared.Services;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AuthTests")]

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

            var sessionData = tokenRepr == null || tokenRepr.IsFailure
                ? UserModel.AnonymousUser()
                : new UserModel
                {
                    UserId = tokenRepr.Value.UserId,
                    UserName = tokenRepr.Value.Login,
                    BlogId = tokenRepr.Value.BlogId,
                };
            userModel = sessionData;
            return userModel;
        }
    }
}
