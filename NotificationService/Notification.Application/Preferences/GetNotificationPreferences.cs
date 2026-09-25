using Infrastructure.Services;
using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Application.Preferences;

public sealed class GetNotificationPreferences(
    INotificationPreferenceStore store,
    ICurrentUserService currentUser)
{
    public async Task<Result<IReadOnlyList<NotificationPreference>>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await Validation.User(currentUser, cancellationToken);
        if (user.IsFailure)
            return Result<IReadOnlyList<NotificationPreference>>.Failure(user.Errors);

        var settings = await store.GetAsync(user.Value, cancellationToken);
        if (settings.IsFailure) return Result<IReadOnlyList<NotificationPreference>>.Failure(settings.Errors);
        return Enum.GetValues<NotificationKind>()
            .Select(kind => new NotificationPreference(kind, DeliveryType.InApp,
                NotificationPreferenceResolver.IsEnabled(kind, DeliveryType.InApp, settings.Value))).ToArray();
    }
}
