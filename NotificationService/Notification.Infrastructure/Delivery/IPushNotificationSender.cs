namespace Notification.Infrastructure.Delivery;

public interface IPushNotificationSender
{
    Task SendAsync(Guid idempotencyKey, string destinationKey, NotificationDeliveryMessage notification,
        CancellationToken cancellationToken = default);
}
