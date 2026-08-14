using Infrastructure.Services;

namespace Recommendation.Application.Services;

public interface IRecommendationSubjectResolver
{
    Task<RecommendationSubject?> ResolveAsync(CancellationToken cancellationToken = default);
}

public sealed record RecommendationSubject(Guid UserId);

public sealed class RecommendationSubjectResolver(ICurrentUserService currentUserService)
    : IRecommendationSubjectResolver
{
    public async Task<RecommendationSubject?> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentUser = await currentUserService.GetCurrentUserAsync();

        return currentUser.IsAnonymous || currentUser.UserId == Guid.Empty
            ? null
            : new RecommendationSubject(currentUser.UserId);
    }
}
