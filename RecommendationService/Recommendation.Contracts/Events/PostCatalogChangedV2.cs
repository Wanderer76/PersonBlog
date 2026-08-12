namespace Recommendation.Contracts.Events;

public sealed record PostCatalogChangedV2
{
    public required Guid EventId { get; init; }
    public required long AggregateVersion { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid PostId { get; init; }
    public required Guid BlogId { get; init; }
    public required RecommendationPostType PostType { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<int> CategoryIds { get; init; } = [];
    public required RecommendationPostVisibility Visibility { get; init; }
    public required RecommendationProcessState ProcessState { get; init; }
    public required bool IsDeleted { get; init; }
    public required bool IsBanned { get; init; }
    public Guid? PaymentSubscriptionId { get; init; }
    public string? PreviewObjectName { get; init; }
    public double? DurationSeconds { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int ViewCount { get; init; }
    public required int LikeCount { get; init; }
    public required int DislikeCount { get; init; }
}

public enum RecommendationPostType
{
    Text,
    Video
}

public enum RecommendationPostVisibility
{
    Public,
    ByUrl,
    Private
}

public enum RecommendationProcessState
{
    Draft,
    Complete,
    Load,
    Error
}
