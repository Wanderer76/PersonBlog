using Notification.Application.Preferences;

namespace Notification.Application.Abstractions;

public interface INotificationPreferenceStore
{
    Task<Result<IReadOnlyList<NotificationPreference>>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Atomically upsert specified keys (UserId, Kind, Channel), preserving unspecified settings.</summary>
    Task<Result> UpdateAsync(Guid userId, IReadOnlyList<NotificationPreference> preferences,
        DateTimeOffset updatedAt, CancellationToken cancellationToken = default);
}
