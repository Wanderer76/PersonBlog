using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public static class AnonymousSession
{
    public const string HeaderName = "X-Anonymous-Session-Id";
    public const string CookieName = "AnonymousSessionId";

    private static readonly object HttpContextItemKey = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static string GetOrCreate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(HttpContextItemKey, out var cached) && cached is string cachedId)
            return cachedId;

        var sessionId = Normalize(context.Request.Headers[HeaderName].FirstOrDefault())
            ?? Normalize(context.Request.Cookies[CookieName])
            ?? Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKey] = sessionId;

        if (!context.Request.Cookies.ContainsKey(CookieName))
        {
            context.Response.Cookies.Append(CookieName, sessionId, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                MaxAge = Lifetime,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
        }

        return sessionId;
    }

    private static string? Normalize(string? value) =>
        Guid.TryParseExact(value?.Trim(), "N", out var sessionId)
            ? sessionId.ToString("N")
            : null;
}
