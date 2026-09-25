using Recommendation.Domain.Enums;

namespace Recommendation.Domain.Models;

public sealed record PostSnapshotData
{
    public required Guid PostId { get; init; }
    public required long SourceVersion { get; init; }
    public required Guid BlogId { get; init; }
    public required PostType PostType { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public IReadOnlyCollection<int> CategoryIds { get; init; } = [];
    public required PostVisibility Visibility { get; init; }
    public required PostProcessState ProcessState { get; init; }
    public required bool IsDeleted { get; init; }
    public required bool IsBanned { get; init; }
    public Guid? PaymentSubscriptionId { get; init; }
    public string? PreviewObjectName { get; init; }
    public double? DurationSeconds { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int ViewCount { get; init; }
    public required int LikeCount { get; init; }
    public required int DislikeCount { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
