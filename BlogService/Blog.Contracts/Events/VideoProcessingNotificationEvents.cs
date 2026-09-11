namespace Blog.Contracts.Events;

public static class VideoProcessingIntegrationEvents
{
    public const string CompletedV1RoutingKey = "video.processing-completed.v1";
    public const string FailedV1RoutingKey = "video.processing-failed.v1";
}

public sealed record VideoProcessingCompletedV1
{
    public const int CurrentSchemaVersion = 1;
    public const string ProducerName = "Blog";

    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Producer { get; init; } = ProducerName;
    public required Guid PostId { get; init; }
    public required Guid BlogId { get; init; }
    public required Guid RecipientUserId { get; init; }
    public required Guid VideoMetadataId { get; init; }
    public required Guid ProcessingAttemptId { get; init; }
}

public sealed record VideoProcessingFailedV1
{
    public const int CurrentSchemaVersion = 1;
    public const string ProducerName = "Blog";

    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Producer { get; init; } = ProducerName;
    public required Guid PostId { get; init; }
    public required Guid BlogId { get; init; }
    public required Guid RecipientUserId { get; init; }
    public required Guid VideoMetadataId { get; init; }
    public required Guid ProcessingAttemptId { get; init; }
    public required string ErrorCode { get; init; }
}
