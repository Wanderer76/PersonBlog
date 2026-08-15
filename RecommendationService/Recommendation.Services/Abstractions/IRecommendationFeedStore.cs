using Recommendation.Domain.Entities;
using Recommendation.Services.Models;

namespace Recommendation.Services.Abstractions;

public interface IRecommendationFeedStore
{
    Task<IReadOnlyList<RecommendationCandidateData>> LoadCandidatesAsync(
        Guid? userId,
        Guid? currentPostId,
        int limit,
        DateTimeOffset seenSince,
        CancellationToken cancellationToken = default);

    Task SaveImpressionsAsync(
        IReadOnlyCollection<RecommendationImpression> impressions,
        CancellationToken cancellationToken = default);
}
