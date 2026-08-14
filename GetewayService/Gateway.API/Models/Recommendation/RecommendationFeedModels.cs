namespace Gateway.API.Models.Recommendation;

public sealed record RecommendationFeedResponse(
    Guid RequestId,
    string AlgorithmVersion,
    IReadOnlyList<RecommendationFeedItem> Items,
    string? NextCursor);

public sealed record RecommendationFeedItem(
    Guid PostId,
    string Reason,
    string Title,
    string? Description,
    string? PreviewUrl);

public sealed record RecommendationRankingResponse(
    Guid RequestId,
    string AlgorithmVersion,
    IReadOnlyList<RecommendationRankingItem> Items,
    string? NextCursor);

public sealed record RecommendationRankingItem(Guid PostId, string Reason);

public sealed record BlogPostCard(
    Guid Id,
    string Title,
    string? Description,
    string? PreviewObjectName);
