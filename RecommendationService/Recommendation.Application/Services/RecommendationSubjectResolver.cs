using System.Security.Claims;

namespace Recommendation.Application.Services;

public interface IRecommendationSubjectResolver
{
    bool TryResolve(HttpContext httpContext, out RecommendationSubject subject, out string? error);
}

public sealed record RecommendationSubject(Guid? UserId, string? AnonymousSessionId);

public sealed class RecommendationSubjectResolver : IRecommendationSubjectResolver
{
    public const string AnonymousSessionHeader = "X-Anonymous-Session-Id";

    public bool TryResolve(
        HttpContext httpContext,
        out RecommendationSubject subject,
        out string? error)
    {
        var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue("user_id");
        if (Guid.TryParse(userIdValue, out var userId) && userId != Guid.Empty)
        {
            subject = new RecommendationSubject(userId, null);
            error = null;
            return true;
        }

        var anonymousSessionId = httpContext.Request.Headers[AnonymousSessionHeader]
            .FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(anonymousSessionId))
        {
            subject = null!;
            error = $"Authenticated user or {AnonymousSessionHeader} header is required.";
            return false;
        }
        if (anonymousSessionId.Length > 200)
        {
            subject = null!;
            error = $"{AnonymousSessionHeader} cannot exceed 200 characters.";
            return false;
        }

        subject = new RecommendationSubject(null, anonymousSessionId);
        error = null;
        return true;
    }
}
