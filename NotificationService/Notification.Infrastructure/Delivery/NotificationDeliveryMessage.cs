using Notification.Domain.Entities;
using Notification.Application.Notifications;

namespace Notification.Infrastructure.Delivery;

public sealed record NotificationDeliveryMessage(
    Guid NotificationId,
    NotificationKind Kind,
    Guid BusinessId,
    Guid? ActorUserId,
    NotificationTarget Target,
    string TemplateKey,
    int TemplateVersion,
    IReadOnlyDictionary<string, string> Data,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt)
{
    internal static NotificationDeliveryMessage From(NotificationItem notification) => new(
        notification.Id,
        notification.Content.Kind,
        notification.Content.BusinessId,
        notification.Content.ActorUserId,
        notification.Content.Target,
        notification.Content.TemplateKey,
        notification.Content.TemplateVersion,
        notification.Content.Data,
        notification.CreatedAt,
        notification.Content.ExpiresAt);
}
