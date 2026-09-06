namespace Notification.Domain.Entities;

public class NotificationType : INotificationEntity
{
    public long Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DeliveryType DeliveryType { get; private set; }

    private NotificationType()
    {
    }

    public NotificationType(long id, string name, bool isActive, DeliveryType deliveryType)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
        DeliveryType = deliveryType;
    }
}
