using Infrastructure.Services;
using Notification.Application.Abstractions;

namespace Notification.Application.Notifications;

public sealed class GetNotifications(INotificationStore store, ICurrentUserService currentUser)
{
    public async Task<Result<NotificationPage>> ExecuteAsync(DateTimeOffset snapshotAt,
        NotificationCursor? cursor = null, int limit = 50, bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var user = await Validation.User(currentUser, cancellationToken);
        if (user.IsFailure) return Result<NotificationPage>.Failure(user.Errors);
        if (limit is < 1 or > 100)
            return Result<NotificationPage>.Failure(
                new Shared.Utils.Error(nameof(limit), "Limit must be between 1 and 100."));
        if (cursor is not null && cursor.CreatedAt > snapshotAt)
            return Result<NotificationPage>.Failure(
                new Shared.Utils.Error(nameof(cursor), "Cursor is outside snapshot."));

        return await store.ListAsync(user.Value, cursor, limit, unreadOnly, snapshotAt, cancellationToken);
    }
}
