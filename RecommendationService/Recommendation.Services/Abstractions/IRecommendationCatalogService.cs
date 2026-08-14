using Recommendation.Services.Models;

namespace Recommendation.Services.Abstractions;

public interface IRecommendationCatalogService
{
    Task<IReadOnlyList<RecommendationPostSummary>> GetPostsByIdsAsync(
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken = default);
}
