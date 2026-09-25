using Notification.Domain.Entities;

namespace Notification.Application.Notifications;

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
