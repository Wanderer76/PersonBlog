using Microsoft.AspNetCore.Http;
using Shared.Services;

namespace Infrastructure.Middleware;

public sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public const string CorrelationId = "X-Correlation-Id";

    public CorrelationMiddleware(RequestDelegate requestDelegate)
    {
        _next = requestDelegate;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(CorrelationId))
        {
            context.Request.Headers[CorrelationId] = GuidService.GetNewGuid().ToString();
        }
        await _next(context);
    }
}
