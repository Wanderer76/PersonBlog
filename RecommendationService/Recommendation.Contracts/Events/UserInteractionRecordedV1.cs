namespace Recommendation.Contracts.Events;

public sealed record UserInteractionRecordedV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public Guid? UserId { get; init; }
    public string? AnonymousSessionId { get; init; }
    public required Guid PostId { get; init; }
    public required UserInteractionType Type { get; init; }
    public double? WatchedSeconds { get; init; }
    public double? WatchRatio { get; init; }
    public bool? Reaction { get; init; }
    public bool? PreviousReaction { get; init; }
}

public enum UserInteractionType
{
    Impression,
    Open,
    ViewProgress,
    ViewCompleted,
    Like,
    Dislike,
    ReactionRemoved
}
