using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Shared.Services;
using Shared.Utils;

namespace Notification.Persistence.Stores;

internal sealed class NotificationStore(StoreOperation operation, IDateTimeManager dateTimeManager)
    : NotificationStoreBase(operation, dateTimeManager), INotificationStore
{
    public Task<Result<CreationResult>> CompleteCreationAsync(InboxCheckpoint checkpoint,
        NotificationDraft? draft, CancellationToken cancellationToken = default) =>
        Operation.Run<CreationResult>(async db =>
        {
            var inbox = await FindInboxAsync(db, checkpoint, cancellationToken);
            if (inbox is null)
                return Result<CreationResult>.Failure(Stale);
            if (draft is not null && draft.Channels.Any(channel => channel != DeliveryType.InApp))
                return Result<CreationResult>.Failure(
                    new Error("Delivery.UnsupportedChannel", "Drafts support InApp only."));

            var result = draft is null
                ? new CreationResult(CreationStatus.Suppressed, null)
                : await InsertAsync(db, draft, cancellationToken);
            return inbox.TryComplete(checkpoint.LeaseToken, Now)
                ? Result<CreationResult>.Success(result)
                : Result<CreationResult>.Failure(Stale);
        }, true, cancellationToken);

    public Task<Result<NotificationPage>> ListAsync(Guid userId, NotificationCursor? cursor, int limit,
        bool unreadOnly, DateTimeOffset snapshotAt, CancellationToken cancellationToken = default) =>
        Operation.Run<NotificationPage>(async db =>
        {
            if (limit is < 1 or > 100)
                return Result<NotificationPage>.Failure(new Error("limit", "Limit must be between 1 and 100."));

            var snapshot = snapshotAt.ToUniversalTime();
            var query = db.Set<UserNotification>().AsNoTracking()
                .Where(notification => notification.UserId == userId && notification.CreatedAt <= snapshot);
            if (unreadOnly)
                query = query.Where(notification => notification.ReadAt == null);
            if (cursor is not null)
            {
                var boundary = cursor.CreatedAt.ToUniversalTime();
                query = query.Where(notification => notification.CreatedAt < boundary ||
                    (notification.CreatedAt == boundary && notification.Id.CompareTo(cursor.Id) < 0));
            }

            var rows = await query.OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id).Take(limit + 1).ToListAsync(cancellationToken);
            var items = rows.Take(limit).Select(Item).ToArray();
            var next = rows.Count > limit ? new NotificationCursor(items[^1].CreatedAt, items[^1].Id) : null;
            return new NotificationPage(items, next, snapshotAt);
        }, false, cancellationToken);

    public Task<Result<long>> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Operation.Run<long>(async db => await db.Set<UserNotification>().LongCountAsync(
            notification => notification.UserId == userId && notification.ReadAt == null, cancellationToken),
            false, cancellationToken);

    public Task<Result<bool>> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt,
        CancellationToken cancellationToken = default) => Operation.Run<bool>(async db =>
        {
            var notification = await db.Set<UserNotification>()
            .SingleOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);
            if (notification is null)
                return false;
            notification.MarkRead(readAt);
            return true;
        }, true, cancellationToken);

    public Task<Result<int>> MarkAllReadAsync(Guid userId, DateTimeOffset snapshotAt, DateTimeOffset readAt,
        CancellationToken cancellationToken = default) => Operation.Run<int>(async db =>
        {
            var snapshot = snapshotAt.ToUniversalTime();
            var timestamp = readAt.ToUniversalTime();
            return await db.Set<UserNotification>()
                .Where(notification => notification.UserId == userId && notification.ReadAt == null &&
                    notification.CreatedAt <= snapshot)
                .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAt, timestamp),
                    cancellationToken);
        }, true, cancellationToken);
}
