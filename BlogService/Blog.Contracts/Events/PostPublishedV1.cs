namespace Blog.Contracts.Events;

public static class BlogIntegrationEvents
{
    public const string Exchange = "blog.events";
    public const string PostPublishedV1RoutingKey = "post.published.v1";
}

public enum PostPublicationAudience
{
    Public,
    ByUrl,
    Private
}

/// <summary>A fact that a post became available to its audience for the first time.</summary>
public sealed record PostPublishedV1
{
    public const int CurrentSchemaVersion = 1;
    public const string ProducerName = "Blog";

    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Producer { get; init; } = ProducerName;
    public required Guid PostId { get; init; }
    public required Guid BlogId { get; init; }
    public required Guid AuthorUserId { get; init; }
    public required Guid PublicationId { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required string Title { get; init; }
    public required PostPublicationAudience Audience { get; init; }
}
