using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;

namespace Recommendation.Services.Services;

public sealed class RecommendationCatalogService(IRecommendationFeedStore store)
    : IRecommendationCatalogService
{
    public async Task<IReadOnlyList<RecommendationPostSummary>> GetPostsByIdsAsync(
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(postIds);
        if (postIds.Count > 100) throw new ArgumentOutOfRangeException(nameof(postIds), "At most 100 posts can be requested.");
        if (postIds.Any(x => x == Guid.Empty)) throw new ArgumentException("Post IDs cannot be empty.", nameof(postIds));
        if (postIds.Count == 0) return [];

        var posts = await store.LoadPostSummariesAsync(postIds, cancellationToken);
        var byId = posts.ToDictionary(x => x.PostId);
        return postIds.Distinct().Where(byId.ContainsKey).Select(id => byId[id]).ToArray();
    }
}
