namespace Recommendation.Contracts.Events;

public sealed record SubscriptionChangedV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid UserId { get; init; }
    public required Guid BlogId { get; init; }
    public required bool IsSubscribed { get; init; }
}
