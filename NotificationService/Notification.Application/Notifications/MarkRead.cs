using Infrastructure.Services;
using Notification.Application.Abstractions;

namespace Notification.Application.Notifications;

public sealed class MarkRead(INotificationStore store, ICurrentUserService currentUser)
{
    public async Task<Result<bool>> ExecuteAsync(Guid notificationId, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var user = await Validation.User(currentUser, cancellationToken);
        if (user.IsFailure) return Result<bool>.Failure(user.Errors);
        var id = Validation.Id(notificationId, nameof(notificationId));
        if (id.IsFailure) return Result<bool>.Failure(id.Errors);

        return await store.MarkReadAsync(user.Value, notificationId, now, cancellationToken);
    }

    /// <summary>The API must recover snapshotAt from a server-issued snapshot boundary.</summary>
    public async Task<Result<int>> AllAsync(DateTimeOffset snapshotAt, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var user = await Validation.User(currentUser, cancellationToken);
        if (user.IsFailure) return Result<int>.Failure(user.Errors);
        if (snapshotAt > now)
            return Result<int>.Failure(
                new Shared.Utils.Error(nameof(snapshotAt), "Snapshot cannot be later than read time."));

        return await store.MarkAllReadAsync(user.Value, snapshotAt, now, cancellationToken);
    }
}
