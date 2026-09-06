using Infrastructure.Services;
using Notification.Application.Abstractions;

namespace Notification.Application.Notifications;

public sealed class CountUnread(INotificationStore store, ICurrentUserService currentUser)
{
    public async Task<Result<long>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var user = await Validation.User(currentUser, cancellationToken);
        if (user.IsFailure) return Result<long>.Failure(user.Errors);

        return await store.CountUnreadAsync(user.Value, cancellationToken);
    }
}
