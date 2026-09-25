using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Application.Preferences;

public sealed record NotificationPreference(NotificationKind Kind, DeliveryType Channel, bool Enabled);

public static class NotificationPreferenceResolver
{
    public static bool IsEnabled(NotificationKind kind, DeliveryType channel,
        IReadOnlyList<NotificationPreference> preferences) =>
        preferences.SingleOrDefault(x => x.Kind == kind && x.Channel == channel)?.Enabled
        ?? channel == DeliveryType.InApp;
}
