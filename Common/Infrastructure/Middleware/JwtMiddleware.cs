using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Models;
using Shared.Services;
using System.Net;

namespace Infrastructure.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly IJwtTokenService _jwtTokenService;

        public JwtMiddleware(RequestDelegate requestDelegate, IConfiguration configuration, ICacheService cacheService, IJwtTokenService jwtTokenService)
        {
            _next = requestDelegate;
            _configuration = configuration;
            _cacheService = cacheService;
            _jwtTokenService = jwtTokenService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestToken = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').Last();

            if (requestToken != null)
            {
                var token = _jwtTokenService.GetTokenModel(requestToken);
                if (token.IsFailure)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.StartAsync();
                }
                var blackList = await _cacheService.GetCachedDataAsync<TokenModel>(new BlacklistTokenCacheKey(token.Value.Id));
                //if (token.Value.ExpiredAt <= DateTimeService.Now() || blackList != null)
                //{
                //    context.Response.ContentType = "application/json";
                //    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                //    await context.Response.StartAsync();
                //}
                var _currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
                var currentUser = await _currentUserService.GetCurrentUserAsync();

                if (currentUser == null)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.StartAsync();
                }
                else
                {
                    context.Items.Add("userId", token.Value.UserId);
                }
            }
            await _next(context);
        }
    }

    public static class JwtMiddlewareExtension
    {
        public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtMiddleware>();
        }
    }
}
