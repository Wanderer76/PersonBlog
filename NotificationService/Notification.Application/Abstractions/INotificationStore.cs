using Notification.Application.Notifications;

namespace Notification.Application.Abstractions;

public interface INotificationStore
{
    /// <summary>
    /// Atomically inserts notification and InApp delivery job and completes the claimed inbox row.
    /// A null draft completes a suppressed event. Enforce (UserId, Kind, BusinessId) uniqueness
    /// in the database; duplicates return the existing id without a new delivery job.
    /// Check the lease token before any mutation; roll back everything if it is stale.
    /// </summary>
    Task<CreationResult> CompleteCreationAsync(InboxCheckpoint checkpoint, NotificationDraft? draft,
        CancellationToken cancellationToken = default);

    /// <summary>Filter by user, snapshot and optional unread flag; use descending CreatedAt/Id keyset pagination.</summary>
    Task<NotificationPage> ListAsync(Guid userId, NotificationCursor? cursor, int limit,
        bool unreadOnly, DateTimeOffset snapshotAt, CancellationToken cancellationToken = default);

    Task<long> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Set ReadAt only if null. Return true for an already read owned row, false for missing/foreign rows.</summary>
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt,
        CancellationToken cancellationToken = default);

    /// <summary>Only update unread owned rows created at or before snapshotAt; preserve existing ReadAt.</summary>
    Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset snapshotAt, DateTimeOffset readAt,
        CancellationToken cancellationToken = default);
}
