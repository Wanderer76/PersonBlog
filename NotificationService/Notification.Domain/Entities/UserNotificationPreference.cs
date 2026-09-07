namespace Notification.Domain.Entities;

public sealed class UserNotificationPreference : INotificationEntity
{
    public Guid UserId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public DeliveryType Channel { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserNotificationPreference() { }

    public static UserNotificationPreference Create(Guid userId, NotificationKind kind, DeliveryType channel,
        bool enabled, DateTimeOffset updatedAt) => new()
    {
        UserId = userId, Kind = kind, Channel = channel, Enabled = enabled,
        UpdatedAt = updatedAt.ToUniversalTime()
    };

    public void SetEnabled(bool enabled, DateTimeOffset updatedAt)
    {
        Enabled = enabled;
        UpdatedAt = updatedAt.ToUniversalTime();
    }
}
