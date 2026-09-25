using Recommendation.Domain.Enums;

namespace Recommendation.Services.Models;

public sealed record RecommendationFeedRequest(
    Guid? UserId,
    string? AnonymousSessionId,
    int Limit,
    string? Cursor,
    Guid? CurrentPostId,
    int InitialOffset = 0,
    PostType PostType = PostType.Video);

public sealed record RecommendationFeedResponse(
    Guid RequestId,
    string AlgorithmVersion,
    IReadOnlyList<RecommendationFeedItem> Items,
    string? NextCursor);

public sealed record RecommendationFeedItem(Guid PostId, string Reason);

public sealed record RecommendationCandidateData(
    Guid PostId,
    Guid BlogId,
    DateTimeOffset CreatedAt,
    int ViewCount,
    int LikeCount,
    int DislikeCount,
    double CategoryAffinity,
    double BlogAffinity,
    double CurrentPostSimilarity,
    bool IsSubscribed,
    bool WasSeen);

internal sealed record RankedRecommendationCandidate(
    RecommendationCandidateData Candidate,
    double Score,
    CandidateSource Source);

public sealed class InvalidRecommendationCursorException(string message) : Exception(message);
