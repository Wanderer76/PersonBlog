using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Claims;

namespace Infrastructure.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authFilter = context.GetEndpoint()?.Metadata.GetMetadata<AuthFilterAttribute>();

        if (context.User.Identity?.IsAuthenticated != true)
        {
            if (authFilter != null)
            {
                await WriteUnauthorizedAsync(context);
                return;
            }

            await _next(context);
            return;
        }

        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
        var currentUser = await currentUserService.GetCurrentUserAsync();

        if (currentUser.IsAnonymous)
        {
            // A structurally valid JWT may refer to an expired or removed session.
            // Public endpoints treat it as an anonymous request; protected endpoints
            // marked with AuthFilter reject it before entering the controller.
            context.User = new ClaimsPrincipal(new ClaimsIdentity());

            if (authFilter != null)
            {
                await WriteUnauthorizedAsync(context);
                return;
            }

            await _next(context);
            return;
        }

        context.Items["userId"] = currentUser.UserId;
        await _next(context);
    }

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return context.Response.CompleteAsync();
    }
}

public static class JwtMiddlewareExtension
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}
