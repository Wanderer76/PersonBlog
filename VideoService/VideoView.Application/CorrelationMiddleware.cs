using Shared.Services;

namespace VideoView.Application
{
    internal class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;

        private const string CorrelationId = "X-Correlation-Id";

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

    public static class CorrelationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationMiddleware>();
        }
    }
}
