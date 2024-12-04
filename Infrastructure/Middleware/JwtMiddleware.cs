using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public JwtMiddleware(RequestDelegate requestDelegate, IConfiguration configuration)
        {
            _next = requestDelegate;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();

            if (requestToken != null)
            {
                var token = JwtUtils.GetTokenRepresentaion(requestToken);
                context.Items.Add("userId", token.UserId);
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
