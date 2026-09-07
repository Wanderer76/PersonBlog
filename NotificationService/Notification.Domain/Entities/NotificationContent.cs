namespace Notification.Domain.Entities;

public enum NotificationKind
{
    VideoProcessingCompleted,
    VideoProcessingFailed,
    PostPublished,
    CommentReply,
    ConferenceInvitation
}

public enum NotificationTargetType { Post, Comment, ConferenceInvitation }

public sealed record NotificationTarget(NotificationTargetType Type, Guid Id);

/// <summary>Small, versioned template data. Adapters must not pass HTML, secrets or raw provider errors.</summary>
public sealed record NotificationContent(
    NotificationKind Kind, Guid BusinessId, Guid? ActorUserId,
    NotificationTarget Target, string TemplateKey, int TemplateVersion,
    IReadOnlyDictionary<string, string> Data, DateTimeOffset OccurredAt,
    DateTimeOffset? ExpiresAt = null);

public sealed record NotificationSource(string Producer, Guid EventId);
