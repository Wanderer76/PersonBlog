using Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace Recommendation.Application.Services;

public interface IRecommendationSubjectResolver
{
    Task<RecommendationSubject?> ResolveAsync(CancellationToken cancellationToken = default);
}

public sealed record RecommendationSubject(Guid? UserId, string? AnonymousSessionId = null);

public sealed class RecommendationSubjectResolver(
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor)
    : IRecommendationSubjectResolver
{
    public async Task<RecommendationSubject?> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentUser = await currentUserService.GetCurrentUserAsync();

        if (!currentUser.IsAnonymous && currentUser.UserId != Guid.Empty)
            return new RecommendationSubject(currentUser.UserId);

        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("An HTTP context is required to resolve an anonymous recommendation subject.");

        return new RecommendationSubject(null, AnonymousSession.GetOrCreate(httpContext));
    }
}
