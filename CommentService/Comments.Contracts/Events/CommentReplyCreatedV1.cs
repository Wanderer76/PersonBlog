namespace Comments.Contracts.Events;

public static class CommentIntegrationEvents
{
    public const string Exchange = "comments.events";
    public const string CommentReplyCreatedV1RoutingKey = "comment.reply-created.v1";
}

public sealed record CommentReplyCreatedV1
{
    public const int CurrentSchemaVersion = 1;
    public const string ProducerName = "Comments";

    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Producer { get; init; } = ProducerName;
    public required Guid CommentId { get; init; }
    public required Guid ParentCommentId { get; init; }
    public required Guid PostId { get; init; }
    public required Guid ActorUserId { get; init; }
    public required Guid RecipientUserId { get; init; }
}
