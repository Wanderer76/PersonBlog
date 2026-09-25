namespace Notification.Infrastructure.Delivery;

public interface INotificationClient
{
    Task NotificationCreated(NotificationDeliveryMessage notification);
}
