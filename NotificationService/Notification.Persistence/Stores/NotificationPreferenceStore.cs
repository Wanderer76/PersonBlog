using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Application.Preferences;
using Notification.Domain.Entities;
using Shared.Services;
using Shared.Utils;

namespace Notification.Persistence.Stores;

internal sealed class NotificationPreferenceStore(StoreOperation operation, IDateTimeManager dateTimeManager)
    : NotificationStoreBase(operation, dateTimeManager), INotificationPreferenceStore
{
    public Task<Result<IReadOnlyList<NotificationPreference>>> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Operation.Run<IReadOnlyList<NotificationPreference>>(async db =>
            await db.Set<UserNotificationPreference>().AsNoTracking()
                .Where(preference => preference.UserId == userId)
                .Select(preference => new NotificationPreference(
                    preference.Kind, preference.Channel, preference.Enabled))
                .ToArrayAsync(cancellationToken), false, cancellationToken);

    public Task<Result> UpdateAsync(Guid userId, IReadOnlyList<NotificationPreference> preferences, DateTimeOffset updatedAt, CancellationToken cancellationToken = default)
        => Operation.Run(async db =>
        {
            if (preferences.Any(preference => preference is null || !Enum.IsDefined(preference.Kind) ||
                    !Enum.IsDefined(preference.Channel)) ||
                preferences.Select(preference => (preference.Kind, preference.Channel)).Distinct().Count() !=
                preferences.Count)
                return Result.Failure("preferences", "Preference keys must be valid and unique.");

            var existing = await db.Set<UserNotificationPreference>()
                .Where(preference => preference.UserId == userId).ToListAsync(cancellationToken);
            foreach (var preference in preferences)
            {
                var entity = existing.SingleOrDefault(item =>
                    item.Kind == preference.Kind && item.Channel == preference.Channel);
                if (entity is null)
                {
                    entity = UserNotificationPreference.Create(userId, preference.Kind, preference.Channel,
                        preference.Enabled, updatedAt);
                    db.Add(entity);
                }
                else
                {
                    entity.SetEnabled(preference.Enabled, updatedAt);
                }
            }

            return Result.Success();
        }, cancellationToken);
}
