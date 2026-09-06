using Infrastructure.Services;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.Application.Preferences;

public sealed class UpdateNotificationPreferences(
    INotificationPreferenceStore store,
    ICurrentUserService currentUser)
{
    public async Task<Result> ExecuteAsync(IReadOnlyList<NotificationPreference>? preferences, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var user = await Validation.User(currentUser, cancellationToken);
        if (user.IsFailure) return Result.Failure(user.Errors);
        if (preferences is null)
            return Result.Failure(nameof(preferences), "Preferences are required.");

        var entries = preferences.ToArray();
        if (entries.Any(x => x is null || !Enum.IsDefined(x.Kind) || x.Channel != DeliveryType.InApp))
            return Result.Failure(nameof(preferences),
                "Only known kinds and the InApp channel are supported in MVP.");
        if (entries.Select(x => (x.Kind, x.Channel)).Distinct().Count() != entries.Length)
            return Result.Failure(nameof(preferences), "Duplicate preference keys.");

        await store.UpdateAsync(user.Value, entries, now, cancellationToken);
        return Result.Success();
    }
}
