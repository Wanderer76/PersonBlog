using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared;
using Shared.Services;
using System.Net;

namespace Infrastructure.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        public JwtMiddleware(RequestDelegate requestDelegate, IConfiguration configuration, ICacheService cacheService)
        {
            _next = requestDelegate;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();

            if (requestToken != null)
            {
                var token = JwtUtils.GetTokenRepresentaion(requestToken);
                var blackList = await _cacheService.GetCachedDataAsync<TokenModel>(new BlacklistToken(token.Id));
                if (token.ExpiredAt <= DateTimeService.Now() || blackList != null)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.StartAsync();
                }
                else
                {
                    context.Items.Add("userId", token.UserId);
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
