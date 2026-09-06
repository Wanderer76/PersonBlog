using Notification.Domain.Entities;

namespace Notification.Application.Notifications;

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

/// <summary>Identifies the claimed inbox row; a stale lease must never complete it.</summary>
public sealed record InboxCheckpoint(NotificationSource Source, string ConsumerName, Guid LeaseToken);

public sealed record NotificationDraft(Guid Id, Guid UserId, NotificationSource Source,
    NotificationContent Content, DateTimeOffset CreatedAt, IReadOnlyList<DeliveryType> Channels);

public sealed record NotificationItem(Guid Id, Guid UserId, NotificationContent Content,
    DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);

/// <summary>Descending keyset boundary, ordered by CreatedAt then Id.</summary>
public sealed record NotificationCursor(DateTimeOffset CreatedAt, Guid Id);

public sealed record NotificationPage(IReadOnlyList<NotificationItem> Items,
    NotificationCursor? NextCursor, DateTimeOffset SnapshotAt);

public enum CreationStatus { Created, Duplicate, Suppressed }
public sealed record CreationResult(CreationStatus Status, Guid? NotificationId);
