using Recommendation.Services.Models;

namespace Recommendation.Services.Abstractions;

public interface IRecommendationFeedService
{
    Task<RecommendationFeedResponse> GetFeedAsync(
        RecommendationFeedRequest request,
        CancellationToken cancellationToken = default);
}
